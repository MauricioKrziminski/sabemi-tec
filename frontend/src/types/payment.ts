export type ProcessingStatus =
  | "pending"
  | "processing"
  | "processed"
  | "invalid"
  | "failed"
  | "permanentlyFailed";

export type PaymentView = "success" | "error" | "pending" | "processing";

export interface PaymentEvent {
  id: string;
  transactionId: string;
  contractId: string | null;
  amount: number | null;
  paymentDate: string | null;
  paymentStatus: string | null;
  processingStatus: ProcessingStatus;
  view: PaymentView;
  errorMessage: string | null;
  attempts: number;
  hasPayloadDivergence: boolean;
  receivedAt: string;
  processedAt: string | null;
}

export interface ContractStatus {
  contractId: string;
  lastStatus: string;
  lastTransactionId: string;
  lastPaymentDate: string;
  totalPaid: number;
  paymentCount: number;
  updatedAt: string;
}

export interface PaymentEventDetails extends PaymentEvent {
  nextAttemptAt: string | null;
  processingStartedAt: string | null;
  correlationId: string | null;
  payload: Record<string, unknown> | null;
  headers: Record<string, unknown> | null;
  contract: ContractStatus | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface MetricsBucket {
  minute: string;
  total: number;
  failures: number;
}

export interface Metrics {
  totalEvents: number;
  processed: number;
  failures: number;
  inProgress: number;
  totalSettled: number;
  contracts: number;
  series: MetricsBucket[];
}

export interface PaymentFilters {
  view: PaymentView | null;
  contractId: string;
  page: number;
}
