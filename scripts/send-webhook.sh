#!/usr/bin/env bash
#
# Assina e envia notificações de pagamento para a API, do mesmo jeito que o banco parceiro faria.
#
#   ./scripts/send-webhook.sh                      envia um pagamento liquidado
#   ./scripts/send-webhook.sh --demo               dispara uma sequência de demonstração
#   ./scripts/send-webhook.sh -t TRX-1 -v 250.90   personaliza os campos
#
set -euo pipefail

URL="${SABEMI_API_URL:-http://localhost:8080}"
SECRET="${WEBHOOK_SIGNING_SECRET:-sabemi-webhook-secret-local}"
TRANSACTION="TRX-$(date +%s)"
CONTRACT="CT-1029"
AMOUNT="1240.00"
STATUS="sucesso"
BODY=""
DEMO="false"

usage() {
  sed -n '3,9p' "$0" | sed 's/^# \{0,1\}//'
  echo
  echo "Opções:"
  echo "  -u, --url        URL base da API (atual: $URL)"
  echo "  -s, --secret     segredo usado na assinatura HMAC"
  echo "  -t, --transacao  identificador da transação"
  echo "  -c, --contrato   identificador do contrato"
  echo "  -v, --valor      valor do pagamento"
  echo "  -e, --status     sucesso ou erro"
  echo "  -b, --body       corpo JSON completo, ignora os demais campos"
  echo "      --demo       envia uma sequência com sucesso, recusa, inválido e duplicado"
  echo "  -h, --help       mostra esta ajuda"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -u|--url) URL="$2"; shift 2 ;;
    -s|--secret) SECRET="$2"; shift 2 ;;
    -t|--transacao) TRANSACTION="$2"; shift 2 ;;
    -c|--contrato) CONTRACT="$2"; shift 2 ;;
    -v|--valor) AMOUNT="$2"; shift 2 ;;
    -e|--status) STATUS="$2"; shift 2 ;;
    -b|--body) BODY="$2"; shift 2 ;;
    --demo) DEMO="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Opção desconhecida: $1" >&2; usage; exit 1 ;;
  esac
done

# A assinatura cobre o carimbo de tempo e o corpo bruto, no formato {timestamp}.{corpo}.
send() {
  local body="$1"
  local label="${2:-}"
  local timestamp
  timestamp="$(date +%s)"

  local signature
  signature="sha256=$(printf '%s.%s' "$timestamp" "$body" \
    | openssl dgst -sha256 -hmac "$SECRET" -r \
    | cut -d' ' -f1)"

  local response
  response="$(curl -s -w '\n%{http_code}' -X POST "$URL/webhooks/pagamento" \
    -H 'Content-Type: application/json' \
    -H "X-Timestamp: $timestamp" \
    -H "X-Signature: $signature" \
    -d "$body")"

  local status="${response##*$'\n'}"
  local payload="${response%$'\n'*}"

  printf '%-28s HTTP %s\n' "${label:-envio}" "$status"
  printf '  %s\n' "$payload"
}

payload() {
  printf '{"id_transacao":"%s","id_contrato":"%s","valor":%s,"data_pagamento":"%s","status":"%s"}' \
    "$1" "$2" "$3" "$(date -u -d '-1 hour' +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u +%Y-%m-%dT%H:%M:%SZ)" "$4"
}

if [[ "$DEMO" == "true" ]]; then
  stamp="$(date +%H%M%S)"

  send "$(payload "TRX-$stamp-A" "CT-1029" "1240.00" "sucesso")" "pagamento liquidado"
  send "$(payload "TRX-$stamp-B" "CT-0771" "320.50" "sucesso")" "pagamento liquidado"
  send "$(payload "TRX-$stamp-C" "CT-0771" "99.90" "erro")" "pagamento recusado"
  send "$(payload "TRX-$stamp-A" "CT-1029" "1240.00" "sucesso")" "reenvio duplicado"
  send '{"id_transacao":"TRX-'"$stamp"'-D","id_contrato":"CT-0140","valor":0,"data_pagamento":"2026-01-10T10:00:00-03:00","status":"pago"}' "payload inválido"

  echo
  echo "Assinatura inválida (espera 401):"
  curl -s -o /dev/null -w '  HTTP %{http_code}\n' -X POST "$URL/webhooks/pagamento" \
    -H 'Content-Type: application/json' \
    -H "X-Timestamp: $(date +%s)" \
    -H 'X-Signature: sha256=0000000000000000000000000000000000000000000000000000000000000000' \
    -d '{"id_transacao":"TRX-FORJADO"}'
  exit 0
fi

if [[ -z "$BODY" ]]; then
  BODY="$(payload "$TRANSACTION" "$CONTRACT" "$AMOUNT" "$STATUS")"
fi

send "$BODY"
