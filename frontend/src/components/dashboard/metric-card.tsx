"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/cn";
import { Skeleton } from "@/components/ui/skeleton";
import { Sparkline } from "./sparkline";

type Tone = "accent" | "positive" | "negative" | "info";

const tones: Record<Tone, { text: string; stroke: string }> = {
  accent: { text: "text-accent", stroke: "var(--color-accent)" },
  positive: { text: "text-positive", stroke: "var(--color-positive)" },
  negative: { text: "text-negative", stroke: "var(--color-negative)" },
  info: { text: "text-info", stroke: "var(--color-info)" },
};

function useAnimatedNumber(value: number, duration = 520) {
  const [current, setCurrent] = useState(value);
  const previous = useRef(value);

  useEffect(() => {
    const from = previous.current;
    const delta = value - from;

    if (delta === 0) return;

    let frame = 0;
    const start = performance.now();

    const tick = (now: number) => {
      const progress = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);

      setCurrent(from + delta * eased);

      if (progress < 1) {
        frame = requestAnimationFrame(tick);
      } else {
        previous.current = value;
      }
    };

    frame = requestAnimationFrame(tick);

    return () => cancelAnimationFrame(frame);
  }, [value, duration]);

  return current;
}

interface MetricCardProps {
  label: string;
  value: number;
  format: (value: number) => string;
  hint?: string;
  tone?: Tone;
  series?: number[];
  loading?: boolean;
}

export function MetricCard({
  label,
  value,
  format,
  hint,
  tone = "accent",
  series,
  loading,
}: MetricCardProps) {
  const animated = useAnimatedNumber(value);
  const palette = tones[tone];

  return (
    <article className="panel group relative overflow-hidden p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-[11px] font-medium uppercase tracking-[0.14em] text-ink-faint">
            {label}
          </p>

          {loading ? (
            <Skeleton className="mt-3 h-8 w-28" />
          ) : (
            <p className={cn("numeric mt-2 text-3xl font-semibold text-ink")}>
              {format(animated)}
            </p>
          )}

          {hint && <p className="mt-1.5 text-xs text-ink-faint">{hint}</p>}
        </div>

        <span
          className={cn(
            "size-2 rounded-full transition-transform group-hover:scale-125",
            palette.text,
          )}
          style={{ backgroundColor: palette.stroke }}
          aria-hidden
        />
      </div>

      {series && series.length > 0 && (
        <Sparkline
          values={series}
          stroke={palette.stroke}
          className="mt-4 h-8 w-full opacity-90"
        />
      )}
    </article>
  );
}
