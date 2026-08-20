"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { PaymentFilters } from "@/types/payment";

export function usePayments(filters: PaymentFilters) {
  return useQuery({
    queryKey: ["payments", filters],
    queryFn: () => api.listPayments(filters),
    placeholderData: keepPreviousData,
  });
}

export function usePaymentDetails(id: string | null) {
  return useQuery({
    queryKey: ["payment", id],
    queryFn: () => api.getPayment(id as string),
    enabled: id !== null,
  });
}
