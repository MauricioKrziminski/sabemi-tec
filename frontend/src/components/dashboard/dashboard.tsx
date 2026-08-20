"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useLivePayments } from "@/hooks/use-live-payments";
import { usePayments } from "@/hooks/use-payments";
import type { PaymentEvent, PaymentFilters, PaymentView } from "@/types/payment";
import { FiltersBar, type ViewFilter } from "./filters-bar";
import { MetricsRow } from "./metrics-row";
import { PaymentDetailDrawer } from "./payment-detail-drawer";
import { PaymentsTable } from "./payments-table";

const views: ViewFilter[] = ["all", "success", "error", "pending"];

export function Dashboard() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const view = readView(searchParams.get("status"));
  const contractParam = searchParams.get("contrato") ?? "";
  const page = Math.max(Number(searchParams.get("pagina") ?? 1), 1);

  const [contractInput, setContractInput] = useState(contractParam);
  const contractId = useDebouncedValue(contractInput);

  const [selected, setSelected] = useState<PaymentEvent | null>(null);
  const closeDetails = useCallback(() => setSelected(null), []);

  const { highlighted } = useLivePayments();

  const filters: PaymentFilters = useMemo(
    () => ({
      view: view === "all" ? null : (view as PaymentView),
      contractId,
      page,
    }),
    [view, contractId, page],
  );

  const { data, isPending, error, refetch } = usePayments(filters);

  const updateParams = useCallback(
    (changes: Record<string, string | null>) => {
      const params = new URLSearchParams(searchParams.toString());

      Object.entries(changes).forEach(([key, value]) => {
        if (value) params.set(key, value);
        else params.delete(key);
      });

      router.replace(params.size > 0 ? `/?${params}` : "/", { scroll: false });
    },
    [router, searchParams],
  );

  const onViewChange = useCallback(
    (next: ViewFilter) => updateParams({ status: next === "all" ? null : next, pagina: null }),
    [updateParams],
  );

  const onContractIdChange = useCallback(
    (value: string) => {
      setContractInput(value);
      updateParams({ contrato: value || null, pagina: null });
    },
    [updateParams],
  );

  const onPageChange = useCallback(
    (next: number) => updateParams({ pagina: next > 1 ? String(next) : null }),
    [updateParams],
  );

  return (
    <div className="mx-auto w-full max-w-6xl px-5 pb-20 pt-10 sm:px-8">
      <header>
        <p className="text-[11px] font-medium uppercase tracking-[0.28em] text-accent">Sabemi</p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-ink sm:text-3xl">
          Liquidações recebidas
        </h1>
        <p className="mt-1.5 max-w-lg text-sm leading-relaxed text-ink-muted">
          Notificações de pagamento do banco parceiro, com a situação de cada evento e do contrato
          correspondente.
        </p>
      </header>

      <div className="mt-8 space-y-6">
        <MetricsRow />

        <FiltersBar
          view={view}
          onViewChange={onViewChange}
          contractId={contractInput}
          onContractIdChange={onContractIdChange}
        />

        <PaymentsTable
          data={data}
          loading={isPending}
          error={error}
          highlighted={highlighted}
          onSelect={setSelected}
          onRetry={() => void refetch()}
          onPageChange={onPageChange}
        />
      </div>

      <p aria-live="polite" className="sr-only">
        {data ? `${data.total} eventos na listagem atual.` : "Carregando eventos."}
      </p>

      <PaymentDetailDrawer payment={selected} onClose={closeDetails} />
    </div>
  );
}

function readView(value: string | null): ViewFilter {
  return views.includes(value as ViewFilter) ? (value as ViewFilter) : "all";
}
