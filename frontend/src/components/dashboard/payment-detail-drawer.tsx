"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { Skeleton } from "@/components/ui/skeleton";
import { usePaymentDetails } from "@/hooks/use-payments";
import { api } from "@/lib/api";
import { formatCurrency, formatDateTime } from "@/lib/format";
import type { PaymentEvent } from "@/types/payment";
import { appearanceFor, StatusPill } from "./status-pill";

interface PaymentDetailDrawerProps {
  payment: PaymentEvent | null;
  onClose: () => void;
}

export function PaymentDetailDrawer({ payment, onClose }: PaymentDetailDrawerProps) {
  const queryClient = useQueryClient();
  const { data, isPending } = usePaymentDetails(payment?.id ?? null);

  const reprocess = useMutation({
    mutationFn: () => api.reprocessPayment(payment!.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["payment", payment?.id] });
      void queryClient.invalidateQueries({ queryKey: ["payments"] });
    },
  });

  const details = data ?? payment;
  const canReprocess =
    details?.processingStatus === "failed" || details?.processingStatus === "permanentlyFailed";

  return (
    <Drawer
      open={payment !== null}
      onClose={onClose}
      title={payment?.transactionId ?? ""}
      subtitle={payment ? `Contrato ${payment.contractId ?? "não informado"}` : undefined}
    >
      {details && (
        <div className="space-y-6">
          <div className="flex flex-wrap items-center gap-3">
            <StatusPill payment={details} />
            <span className="numeric text-2xl font-semibold text-ink">
              {formatCurrency(details.amount)}
            </span>
          </div>

          {details.errorMessage && (
            <div className="flex items-start gap-3 rounded-xl border border-negative/30 bg-negative-soft/60 p-4">
              <AlertTriangle className="mt-0.5 size-4 shrink-0 text-negative" aria-hidden />
              <div className="space-y-1">
                <p className="text-xs font-medium text-negative">Evento com alerta</p>
                <p className="text-xs leading-relaxed text-ink-muted">{details.errorMessage}</p>
              </div>
            </div>
          )}

          {details.hasPayloadDivergence && (
            <div className="rounded-xl border border-warning/30 bg-warning-soft/50 p-4 text-xs leading-relaxed text-ink-muted">
              Esta transação foi reenviada com um corpo diferente do original. O primeiro
              recebimento continua valendo, e a divergência ficou registrada para auditoria.
            </div>
          )}

          <Section title="Processamento">
            <Field label="Situação" value={appearanceFor(details).label} />
            <Field label="Resultado informado" value={details.paymentStatus ?? "-"} />
            <Field label="Tentativas" value={String(details.attempts)} mono />
            <Field label="Recebido em" value={formatDateTime(details.receivedAt)} />
            <Field label="Processado em" value={formatDateTime(details.processedAt)} />
            {isPending ? (
              <Skeleton className="h-4 w-40" />
            ) : (
              data && (
                <>
                  <Field label="Próxima tentativa" value={formatDateTime(data.nextAttemptAt)} />
                  <Field label="Correlação" value={data.correlationId ?? "-"} mono />
                </>
              )
            )}
          </Section>

          {data?.contract && (
            <Section title="Situação do contrato">
              <Field label="Contrato" value={data.contract.contractId} mono />
              <Field label="Último status" value={data.contract.lastStatus} />
              <Field label="Total liquidado" value={formatCurrency(data.contract.totalPaid)} mono />
              <Field label="Pagamentos" value={String(data.contract.paymentCount)} mono />
            </Section>
          )}

          <div className="space-y-2">
            <h3 className="text-[11px] font-medium uppercase tracking-[0.14em] text-ink-faint">
              Payload original
            </h3>
            {isPending && !data ? (
              <Skeleton className="h-40 w-full" />
            ) : (
              <pre className="numeric max-h-72 overflow-auto rounded-xl border border-line bg-canvas p-4 text-xs leading-relaxed text-ink-muted">
                {JSON.stringify(data?.payload ?? {}, null, 2)}
              </pre>
            )}
          </div>

          {canReprocess && (
            <div className="flex items-center justify-between gap-4 rounded-xl border border-line bg-surface-raised p-4">
              <p className="text-xs leading-relaxed text-ink-muted">
                O processamento falhou. É possível devolver este evento para a fila.
              </p>
              <Button
                variant="primary"
                size="sm"
                onClick={() => reprocess.mutate()}
                disabled={reprocess.isPending}
              >
                <RefreshCw
                  className={reprocess.isPending ? "size-3.5 animate-spin" : "size-3.5"}
                  aria-hidden
                />
                Reprocessar
              </Button>
            </div>
          )}
        </div>
      )}
    </Drawer>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-2">
      <h3 className="text-[11px] font-medium uppercase tracking-[0.14em] text-ink-faint">
        {title}
      </h3>
      <dl className="divide-y divide-line rounded-xl border border-line">{children}</dl>
    </section>
  );
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-4 px-4 py-2.5">
      <dt className="text-xs text-ink-faint">{label}</dt>
      <dd className={mono ? "numeric text-xs text-ink" : "text-xs text-ink"}>{value}</dd>
    </div>
  );
}
