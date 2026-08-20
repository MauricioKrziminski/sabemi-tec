"use client";

import { AnimatePresence } from "motion/react";
import { Inbox, ServerCrash } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { Skeleton } from "@/components/ui/skeleton";
import { formatNumber } from "@/lib/format";
import type { PagedResult, PaymentEvent } from "@/types/payment";
import { paymentColumns, paymentGrid } from "./payment-columns";
import { PaymentRow } from "./payment-row";

interface PaymentsTableProps {
  data: PagedResult<PaymentEvent> | undefined;
  loading: boolean;
  error: Error | null;
  highlighted: Set<string>;
  onSelect: (payment: PaymentEvent) => void;
  onRetry: () => void;
  onPageChange: (page: number) => void;
}

export function PaymentsTable({
  data,
  loading,
  error,
  highlighted,
  onSelect,
  onRetry,
  onPageChange,
}: PaymentsTableProps) {
  if (error) {
    return (
      <Placeholder
        icon={<ServerCrash className="size-5 text-negative" aria-hidden />}
        title="Não foi possível carregar os eventos"
        description={error.message}
        action={
          <Button onClick={onRetry} variant="outline" size="sm">
            Tentar novamente
          </Button>
        }
      />
    );
  }

  if (loading && !data) {
    return (
      <div className="panel divide-y divide-line">
        {Array.from({ length: 6 }, (_, index) => (
          <div key={index} className="flex items-center gap-4 px-5 py-4">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-4 w-20" />
            <Skeleton className="ml-auto h-4 w-24" />
            <Skeleton className="h-6 w-24 rounded-full" />
          </div>
        ))}
      </div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <Placeholder
        icon={<Inbox className="size-5 text-ink-faint" aria-hidden />}
        title="Nenhum evento por aqui"
        description="Assim que o banco parceiro enviar uma notificação, ela aparece nesta lista em tempo real."
      />
    );
  }

  return (
    <div className="panel overflow-hidden">
      <div
        aria-hidden
        className={cn(paymentGrid, "hidden border-b border-line px-5 py-2.5 md:grid")}
      >
        {paymentColumns.map((column) => (
          <span
            key={column.label}
            className={cn("text-[11px] uppercase tracking-[0.12em] text-ink-faint", column.align)}
          >
            {column.label}
          </span>
        ))}
        <span />
      </div>

      <ul>
        <AnimatePresence initial={false}>
          {data.items.map((payment) => (
            <PaymentRow
              key={payment.id}
              payment={payment}
              highlighted={highlighted.has(payment.id)}
              onSelect={onSelect}
            />
          ))}
        </AnimatePresence>
      </ul>

      <footer className="flex items-center justify-between gap-4 border-t border-line px-5 py-3">
        <p className="text-xs text-ink-faint">
          Página {data.page} de {Math.max(data.totalPages, 1)}
          <span className="mx-2 text-line-strong">|</span>
          {formatNumber(data.total)} {data.total === 1 ? "evento" : "eventos"}
        </p>

        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="ghost"
            disabled={data.page <= 1}
            onClick={() => onPageChange(data.page - 1)}
          >
            Anterior
          </Button>
          <Button
            size="sm"
            variant="ghost"
            disabled={data.page >= data.totalPages}
            onClick={() => onPageChange(data.page + 1)}
          >
            Próxima
          </Button>
        </div>
      </footer>
    </div>
  );
}

function Placeholder({
  icon,
  title,
  description,
  action,
}: {
  icon: React.ReactNode;
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="panel flex flex-col items-center justify-center gap-3 px-6 py-16 text-center">
      <div className="flex size-11 items-center justify-center rounded-full border border-line bg-surface-raised">
        {icon}
      </div>
      <div className="space-y-1">
        <p className="text-sm font-medium text-ink">{title}</p>
        <p className="mx-auto max-w-sm text-xs leading-relaxed text-ink-faint">{description}</p>
      </div>
      {action}
    </div>
  );
}
