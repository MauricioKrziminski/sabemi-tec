export const paymentGrid =
  "grid grid-cols-2 gap-x-4 gap-y-1 md:grid-cols-[minmax(0,1.15fr)_minmax(0,0.95fr)_minmax(0,0.85fr)_minmax(0,0.65fr)_minmax(0,auto)_1rem] md:gap-x-8";

export const paymentColumns = [
  { label: "Transação", align: "text-left" },
  { label: "Contrato", align: "text-left" },
  { label: "Valor", align: "text-right" },
  { label: "Recebido", align: "text-left" },
  { label: "Situação", align: "text-right" },
] as const;
