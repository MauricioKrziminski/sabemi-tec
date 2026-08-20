"use client";

import { useMetrics } from "@/hooks/use-metrics";
import { formatCompactCurrency, formatNumber } from "@/lib/format";
import { MetricCard } from "./metric-card";

export function MetricsRow() {
  const { data, isPending } = useMetrics();

  const series = data?.series ?? [];
  const totals = series.map((bucket) => bucket.total);
  const failures = series.map((bucket) => bucket.failures);

  return (
    <section
      aria-label="Resumo das liquidações"
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4"
    >
      <MetricCard
        label="Eventos recebidos"
        value={data?.totalEvents ?? 0}
        format={(value) => formatNumber(Math.round(value))}
        hint="Notificações do banco parceiro"
        tone="accent"
        series={totals}
        loading={isPending}
      />
      <MetricCard
        label="Valor liquidado"
        value={data?.totalSettled ?? 0}
        format={formatCompactCurrency}
        hint={`${formatNumber(data?.contracts ?? 0)} contratos atualizados`}
        tone="positive"
        loading={isPending}
      />
      <MetricCard
        label="Com alerta"
        value={data?.failures ?? 0}
        format={(value) => formatNumber(Math.round(value))}
        hint="Recusas e falhas de validação"
        tone="negative"
        series={failures}
        loading={isPending}
      />
      <MetricCard
        label="Em processamento"
        value={data?.inProgress ?? 0}
        format={(value) => formatNumber(Math.round(value))}
        hint="Na fila ou rodando em background"
        tone="info"
        loading={isPending}
      />
    </section>
  );
}
