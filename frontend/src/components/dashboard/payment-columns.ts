export const paymentGrid =
  "grid grid-cols-2 gap-x-4 gap-y-1 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_7rem_5.5rem_8.5rem_1rem] md:gap-x-6";

export const paymentColumns = [
  { label: "Transação", align: "text-left" },
  { label: "Contrato", align: "text-left" },
  { label: "Valor", align: "text-right" },
  { label: "Recebido", align: "text-right" },
  { label: "Situação", align: "text-right" },
] as const;
