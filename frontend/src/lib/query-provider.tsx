"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";

export function QueryProvider({ children }: { children: ReactNode }) {
  // O cliente nasce dentro do estado para não ser compartilhado entre requisições no servidor.
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            // As atualizações chegam pelo SignalR, então o polling fica desligado por padrão.
            refetchOnWindowFocus: true,
            retry: 1,
            staleTime: 5_000,
          },
        },
      }),
  );

  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
