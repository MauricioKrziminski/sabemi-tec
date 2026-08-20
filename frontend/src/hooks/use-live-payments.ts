"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import { API_URL } from "@/lib/api";
import type { PaymentEvent } from "@/types/payment";

const HIGHLIGHT_DURATION = 2_400;
const REFRESH_DEBOUNCE = 250;

interface LivePayments {
  /** Eventos que acabaram de chegar ou mudar, usados para o realce visual da linha. */
  highlighted: Set<string>;
}

/**
 * Assina o hub de pagamentos e mantém as consultas em dia.
 *
 * Em vez de reescrever o cache com o payload recebido, o hook invalida as consultas e deixa a
 * fonte da verdade no servidor. Isso mantém a lista coerente com os filtros ativos e com a
 * paginação, que o evento isolado não conhece.
 */
export function useLivePayments(): LivePayments {
  const queryClient = useQueryClient();
  const [highlighted, setHighlighted] = useState<Set<string>>(() => new Set());

  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const highlightTimers = useRef(new Map<string, ReturnType<typeof setTimeout>>());

  const refresh = useCallback(() => {
    if (refreshTimer.current) clearTimeout(refreshTimer.current);

    // Uma rajada de notificações vira uma única atualização.
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

    // O que aconteceu durante a queda não chegou por evento, então a lista é buscada de novo.
    connection.onreconnected(() => refresh());

    const started = connection.start().catch(() => {
      // Sem tempo real a lista continua funcionando pelas consultas comuns.
    });

    const timers = highlightTimers.current;

    return () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
      timers.forEach((timer) => clearTimeout(timer));
      timers.clear();

      // Encerrar antes de a negociação terminar deixa a conexão em estado inconsistente,
      // então o encerramento espera o início se resolver.
      void started.finally(() => {
        if (connection.state !== HubConnectionState.Disconnected) {
          void connection.stop();
        }
      });
    };
  }, [highlight, refresh]);

  return { highlighted };
}
