import { AlertTriangle, Ban, CheckCircle2, Clock, Loader2, ShieldAlert } from "lucide-react";
import type { ComponentType } from "react";
import { cn } from "@/lib/cn";
import type { PaymentEvent, ProcessingStatus } from "@/types/payment";

interface Appearance {
  label: string;
  icon: ComponentType<{ className?: string }>;
  className: string;
  spin?: boolean;
}

const appearances: Record<ProcessingStatus, Appearance> = {
  pending: {
    label: "Pendente",
    icon: Clock,
    className: "bg-warning-soft text-warning",
  },
  processing: {
    label: "Processando",
    icon: Loader2,
    className: "bg-info-soft text-info",
    spin: true,
  },
  processed: {
    label: "Liquidado",
    icon: CheckCircle2,
    className: "bg-positive-soft text-positive",
  },
  invalid: {
    label: "Inválido",
    icon: ShieldAlert,
    className: "bg-negative-soft text-negative",
  },
  failed: {
    label: "Falhou",
    icon: AlertTriangle,
    className: "bg-negative-soft text-negative",
  },
  permanentlyFailed: {
    label: "Falha final",
    icon: Ban,
    className: "bg-negative-soft text-negative",
  },
};

const declined: Appearance = {
  label: "Recusado",
  icon: Ban,
  className: "bg-negative-soft text-negative",
};

export function appearanceFor(payment: Pick<PaymentEvent, "processingStatus" | "view">) {
  return payment.processingStatus === "processed" && payment.view === "error"
    ? declined
    : appearances[payment.processingStatus];
}

export function StatusPill({
  payment,
  className,
}: {
  payment: Pick<PaymentEvent, "processingStatus" | "view">;
  className?: string;
}) {
  const appearance = appearanceFor(payment);
  const Icon = appearance.icon;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-medium",
        appearance.className,
        className,
      )}
    >
      <Icon className={cn("size-3", appearance.spin && "animate-spin")} aria-hidden />
      {appearance.label}
    </span>
  );
}
