<#
.SYNOPSIS
Assina e envia notificações de pagamento para a API, do mesmo jeito que o banco parceiro faria.

.EXAMPLE
./scripts/send-webhook.ps1
Envia um pagamento liquidado.

.EXAMPLE
./scripts/send-webhook.ps1 -Demo
Dispara a sequência de demonstração com sucesso, recusa, inválido e duplicado.

.EXAMPLE
./scripts/send-webhook.ps1 -Transacao TRX-1 -Valor 250.90 -Status erro
#>
[CmdletBinding()]
param(
    [string] $Url = $(if ($env:SABEMI_API_URL) { $env:SABEMI_API_URL } else { 'http://localhost:8080' }),
    [string] $Secret = $(if ($env:WEBHOOK_SIGNING_SECRET) { $env:WEBHOOK_SIGNING_SECRET } else { 'sabemi-webhook-secret-local' }),
    [string] $Transacao = "TRX-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())",
    [string] $Contrato = 'CT-1029',
    [decimal] $Valor = 1240.00,
    [ValidateSet('sucesso', 'erro')]
    [string] $Status = 'sucesso',
    [string] $Body,
    [switch] $Demo
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Net.Http
$script:HttpClient = [System.Net.Http.HttpClient]::new()

function New-Payload {
    param([string] $Transacao, [string] $Contrato, [decimal] $Valor, [string] $Status)

    $pagamento = [DateTimeOffset]::UtcNow.AddHours(-1).ToString('yyyy-MM-ddTHH:mm:ssZ')
    $valorTexto = $Valor.ToString([System.Globalization.CultureInfo]::InvariantCulture)

    return "{""id_transacao"":""$Transacao"",""id_contrato"":""$Contrato"",""valor"":$valorTexto,""data_pagamento"":""$pagamento"",""status"":""$Status""}"
}

function Send-Webhook {
    param([string] $Body, [string] $Rotulo = 'envio', [string] $Assinatura)

    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

    if (-not $Assinatura) {
        # A assinatura cobre o carimbo de tempo e o corpo bruto, no formato {timestamp}.{corpo}.
        $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($Secret))
        try {
            $hash = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$timestamp.$Body"))
        }
        finally {
            $hmac.Dispose()
        }

        $hex = ($hash | ForEach-Object { $_.ToString('x2') }) -join ''
        $Assinatura = "sha256=$hex"
    }

    # HttpClient em vez de Invoke-WebRequest porque o tratamento de respostas 4xx muda entre
    # o Windows PowerShell 5.1 e o PowerShell 7.
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post, "$Url/webhooks/pagamento")
    $request.Content = [System.Net.Http.StringContent]::new(
        $Body, [Text.Encoding]::UTF8, 'application/json')
    $request.Headers.Add('X-Timestamp', "$timestamp")
    $request.Headers.Add('X-Signature', $Assinatura)

    try {
        $response = $script:HttpClient.SendAsync($request).GetAwaiter().GetResult()
        $status = [int] $response.StatusCode
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    }
    catch {
        Write-Host ("{0,-28} falhou: {1}" -f $Rotulo, $_.Exception.Message) -ForegroundColor Red
        return
    }
    finally {
        $request.Dispose()
    }

    $cor = if ($status -lt 400) { 'Green' } else { 'Yellow' }
    Write-Host ("{0,-28} HTTP {1}" -f $Rotulo, $status) -ForegroundColor $cor

    if ($content) {
        Write-Host "  $content"
    }
}

if ($Demo) {
    $marca = Get-Date -Format 'HHmmss'

    Send-Webhook (New-Payload "TRX-$marca-A" 'CT-1029' 1240.00 'sucesso') 'pagamento liquidado'
    Send-Webhook (New-Payload "TRX-$marca-B" 'CT-0771' 320.50 'sucesso') 'pagamento liquidado'
    Send-Webhook (New-Payload "TRX-$marca-C" 'CT-0771' 99.90 'erro') 'pagamento recusado'
    Send-Webhook (New-Payload "TRX-$marca-A" 'CT-1029' 1240.00 'sucesso') 'reenvio duplicado'

    $invalido = "{""id_transacao"":""TRX-$marca-D"",""id_contrato"":""CT-0140"",""valor"":0,""data_pagamento"":""2026-01-10T10:00:00-03:00"",""status"":""pago""}"
    Send-Webhook $invalido 'payload inválido'

    Send-Webhook '{"id_transacao":"TRX-FORJADO"}' 'assinatura inválida' ('sha256=' + ('0' * 64))
    return
}

if (-not $Body) {
    $Body = New-Payload $Transacao $Contrato $Valor $Status
}

Send-Webhook $Body
