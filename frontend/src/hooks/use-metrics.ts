"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";

export function useMetrics() {
  return useQuery({
    queryKey: ["metrics"],
    queryFn: api.getMetrics,
  });
}
