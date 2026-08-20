"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import { API_URL } from "@/lib/api";
import type { PaymentEvent } from "@/types/payment";

const HIGHLIGHT_DURATION = 2_400;
const REFRESH_DEBOUNCE = 250;

interface LivePayments {
  highlighted: Set<string>;
}

export function useLivePayments(): LivePayments {
  const queryClient = useQueryClient();
  const [highlighted, setHighlighted] = useState<Set<string>>(() => new Set());

  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const highlightTimers = useRef(new Map<string, ReturnType<typeof setTimeout>>());

  const refresh = useCallback(() => {
    if (refreshTimer.current) clearTimeout(refreshTimer.current);

    refreshTimer.current = setTimeout(() => {
      void queryClient.invalidateQueries({ queryKey: ["payments"] });
      void queryClient.invalidateQueries({ queryKey: ["metrics"] });
    }, REFRESH_DEBOUNCE);
  }, [queryClient]);

  const highlight = useCallback((id: string) => {
    setHighlighted((current) => new Set(current).add(id));

    const existing = highlightTimers.current.get(id);
    if (existing) clearTimeout(existing);

    highlightTimers.current.set(
      id,
      setTimeout(() => {
        setHighlighted((current) => {
          const next = new Set(current);
          next.delete(id);
          return next;
        });
        highlightTimers.current.delete(id);
      }, HIGHLIGHT_DURATION),
    );
  }, []);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/payments`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const onEvent = (payment: PaymentEvent) => {
      highlight(payment.id);
      refresh();
    };

    connection.on("paymentReceived", onEvent);
    connection.on("paymentUpdated", onEvent);

    connection.onreconnected(() => refresh());

    const started = connection.start().catch(() => {
    });

    const timers = highlightTimers.current;

    return () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
      timers.forEach((timer) => clearTimeout(timer));
      timers.clear();

      void started.finally(() => {
        if (connection.state !== HubConnectionState.Disconnected) {
          void connection.stop();
        }
      });
    };
  }, [highlight, refresh]);

  return { highlighted };
}
