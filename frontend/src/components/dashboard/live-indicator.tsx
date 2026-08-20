"use client";

import { useEffect, useState } from "react";
import { cn } from "@/lib/cn";
import { formatRelative } from "@/lib/format";
import type { LiveStatus } from "@/hooks/use-live-payments";

const labels: Record<LiveStatus, string> = {
  connecting: "Conectando",
  live: "Ao vivo",
  reconnecting: "Reconectando",
  offline: "Sem conexão",
};

const dots: Record<LiveStatus, string> = {
  connecting: "bg-ink-faint",
  live: "bg-positive",
  reconnecting: "bg-warning",
  offline: "bg-negative",
};

export function LiveIndicator({
  status,
  lastEventAt,
}: {
  status: LiveStatus;
  lastEventAt: Date | null;
}) {
  const [, setTick] = useState(0);

  // Mantém o texto "há Ns" andando sem depender de um novo evento.
  useEffect(() => {
    const timer = setInterval(() => setTick((value) => value + 1), 1_000);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="flex items-center gap-2.5 rounded-full border border-line bg-surface px-3 py-1.5">
      <span className="relative flex size-2">
        {status === "live" && (
          <span className="absolute inline-flex size-full animate-pulse-ring rounded-full bg-positive" />
        )}
        <span className={cn("relative inline-flex size-2 rounded-full", dots[status])} />
      </span>

      <span className="text-xs font-medium text-ink-muted">{labels[status]}</span>

      {lastEventAt && status === "live" && (
        <span className="text-xs text-ink-faint">
          último evento {formatRelative(lastEventAt.toISOString())}
        </span>
      )}
    </div>
  );
}
