"use client";

import { AlertTriangle, ChevronRight, GitCompareArrows } from "lucide-react";
import { motion } from "motion/react";
import { cn } from "@/lib/cn";
import { formatCurrency, formatDateTime, formatRelative } from "@/lib/format";
import type { PaymentEvent } from "@/types/payment";
import { StatusPill } from "./status-pill";

interface PaymentRowProps {
  payment: PaymentEvent;
  highlighted: boolean;
  onSelect: (payment: PaymentEvent) => void;
}

export function PaymentRow({ payment, highlighted, onSelect }: PaymentRowProps) {
  const failing = payment.view === "error";

  return (
    <motion.li
      layout
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.24, ease: "easeOut" }}
      className={cn(
        "border-b border-line last:border-b-0",
        highlighted && "animate-flash",
      )}
    >
      <button
        onClick={() => onSelect(payment)}
        aria-label={`Abrir detalhe da transação ${payment.transactionId}`}
        className={cn(
          "group grid w-full grid-cols-2 items-center gap-x-4 gap-y-1 px-4 py-3 text-left",
          "transition-colors hover:bg-surface-hover",
          "md:grid-cols-[1.1fr_0.9fr_0.9fr_0.8fr_auto_auto] md:px-5",
          failing && "border-l-2 border-l-negative",
        )}
      >
        <span className="numeric text-sm text-ink">{payment.transactionId}</span>

        <span className="numeric justify-self-end text-xs text-ink-muted md:justify-self-start md:text-sm">
          {payment.contractId ?? "-"}
        </span>

        <span
          className={cn(
            "numeric text-sm md:text-right",
            failing ? "text-ink-muted" : "text-ink",
          )}
        >
          {formatCurrency(payment.amount)}
        </span>

        <span
          className="justify-self-end text-xs text-ink-faint md:justify-self-start"
          title={formatDateTime(payment.receivedAt)}
        >
          {formatRelative(payment.receivedAt)}
        </span>

        <span className="col-span-2 flex items-center gap-2 md:col-span-1 md:justify-self-end">
          <StatusPill payment={payment} />
          {payment.attempts > 1 && (
            <span
              className="numeric text-[10px] text-ink-faint"
              title={`${payment.attempts} tentativas de processamento`}
            >
              {payment.attempts}x
            </span>
          )}
          {payment.hasPayloadDivergence && (
            <GitCompareArrows
              className="size-3.5 text-warning"
              aria-label="Reenvio com corpo diferente do original"
            />
          )}
        </span>

        <ChevronRight
          className="hidden size-4 text-ink-faint transition-transform group-hover:translate-x-0.5 group-hover:text-ink-muted md:block"
          aria-hidden
        />

        {payment.errorMessage && (
          <span className="col-span-2 flex items-start gap-2 pt-1 md:col-span-6">
            <AlertTriangle className="mt-0.5 size-3.5 shrink-0 text-negative" aria-hidden />
            <span className="text-xs leading-relaxed text-negative/90">{payment.errorMessage}</span>
          </span>
        )}
      </button>
    </motion.li>
  );
}
