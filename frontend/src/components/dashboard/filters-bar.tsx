"use client";

import { useEffect, useRef } from "react";
import { SearchInput } from "@/components/ui/search-input";
import { SegmentedControl, type SegmentedOption } from "@/components/ui/segmented-control";
import type { PaymentView } from "@/types/payment";

export type ViewFilter = PaymentView | "all";

const options: SegmentedOption<ViewFilter>[] = [
  { value: "all", label: "Todos" },
  { value: "success", label: "Sucesso" },
  { value: "error", label: "Erro" },
  { value: "pending", label: "Pendentes" },
];

interface FiltersBarProps {
  view: ViewFilter;
  onViewChange: (view: ViewFilter) => void;
  contractId: string;
  onContractIdChange: (value: string) => void;
}

export function FiltersBar({
  view,
  onViewChange,
  contractId,
  onContractIdChange,
}: FiltersBarProps) {
  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      const typing = target?.tagName === "INPUT" || target?.tagName === "TEXTAREA";

      if (event.key === "/" && !typing) {
        event.preventDefault();
        searchRef.current?.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, []);

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <SegmentedControl
        label="Filtrar por situação"
        options={options}
        value={view}
        onChange={onViewChange}
      />

      <SearchInput
        ref={searchRef}
        label="Buscar por identificador do contrato"
        placeholder="Buscar contrato"
        value={contractId}
        onChange={onContractIdChange}
        className="sm:w-72"
      />
    </div>
  );
}
