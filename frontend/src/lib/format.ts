const currency = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

const compactCurrency = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
  notation: "compact",
  maximumFractionDigits: 1,
});

const integer = new Intl.NumberFormat("pt-BR");

const dateTime = new Intl.DateTimeFormat("pt-BR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const time = new Intl.DateTimeFormat("pt-BR", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
});

export const formatCurrency = (value: number | null | undefined) =>
  value === null || value === undefined ? "-" : currency.format(value);

export const formatCompactCurrency = (value: number) => compactCurrency.format(value);

export const formatNumber = (value: number) => integer.format(value);

export const formatDateTime = (value: string | null | undefined) =>
  value ? dateTime.format(new Date(value)) : "-";

export const formatTime = (value: string | null | undefined) =>
  value ? time.format(new Date(value)) : "-";

/** Distância curta em relação a agora, no estilo "há 12s". */
export function formatRelative(value: string | null | undefined, now = Date.now()) {
  if (!value) return "-";

  const seconds = Math.max(0, Math.round((now - new Date(value).getTime()) / 1000));

  if (seconds < 60) return `há ${seconds}s`;
  if (seconds < 3600) return `há ${Math.floor(seconds / 60)}min`;
  if (seconds < 86400) return `há ${Math.floor(seconds / 3600)}h`;

  return `há ${Math.floor(seconds / 86400)}d`;
}
