import type {
  ContractStatus,
  Metrics,
  PagedResult,
  PaymentEvent,
  PaymentEventDetails,
  PaymentFilters,
} from "@/types/payment";

export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export const PAGE_SIZE = 15;

/** Erro de API com o status HTTP preservado, para a interface reagir de acordo. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { Accept: "application/json", ...init?.headers },
  });

  if (!response.ok) {
    throw new ApiError(await describe(response), response.status);
  }

  // Nem toda resposta de sucesso traz corpo. O reprocessamento, por exemplo, responde 202 sem
  // conteúdo, e tentar interpretar isso como JSON quebrava a chamada depois de ela ter dado certo.
  if (response.status === 204 || response.headers.get("content-length") === "0") {
    return undefined as T;
  }

  const body = await response.text();

  return (body ? JSON.parse(body) : undefined) as T;
}

async function describe(response: Response) {
  try {
    const problem = (await response.json()) as { title?: string; detail?: string };
    return problem.detail ?? problem.title ?? `Falha na requisição (${response.status}).`;
  } catch {
    return `Falha na requisição (${response.status}).`;
  }
}

export const api = {
  listPayments(filters: PaymentFilters) {
    const params = new URLSearchParams({
      page: String(filters.page),
      pageSize: String(PAGE_SIZE),
    });

    if (filters.view) params.set("status", filters.view);
    if (filters.contractId) params.set("contractId", filters.contractId);

    return request<PagedResult<PaymentEvent>>(`/api/payments?${params}`);
  },

  getPayment(id: string) {
    return request<PaymentEventDetails>(`/api/payments/${id}`);
  },

  reprocessPayment(id: string) {
    return request<void>(`/api/payments/${id}/reprocess`, { method: "POST" });
  },

  listContracts(contractId?: string) {
    const params = new URLSearchParams({ pageSize: "10" });
    if (contractId) params.set("contractId", contractId);

    return request<PagedResult<ContractStatus>>(`/api/contracts?${params}`);
  },

  getMetrics() {
    return request<Metrics>("/api/metrics");
  },
};
