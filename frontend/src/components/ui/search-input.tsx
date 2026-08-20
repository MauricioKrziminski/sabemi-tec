"use client";

import { Search, X } from "lucide-react";
import { forwardRef } from "react";
import { cn } from "@/lib/cn";

interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label: string;
  className?: string;
}

/** Campo de busca com atalho visível e ação de limpar. */
export const SearchInput = forwardRef<HTMLInputElement, SearchInputProps>(function SearchInput(
  { value, onChange, placeholder, label, className },
  ref,
) {
  return (
    <div className={cn("relative flex items-center", className)}>
      <Search className="pointer-events-none absolute left-3 size-4 text-ink-faint" aria-hidden />
      <input
        ref={ref}
        type="search"
        aria-label={label}
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className={cn(
          "h-10 w-full rounded-xl border border-line bg-surface pl-9 pr-16 text-sm text-ink",
          "placeholder:text-ink-faint focus:border-accent/60 focus:outline-none",
          "transition-colors [&::-webkit-search-cancel-button]:hidden",
        )}
      />
      {value ? (
        <button
          onClick={() => onChange("")}
          aria-label="Limpar busca"
          className="absolute right-3 rounded-md p-1 text-ink-faint transition-colors hover:text-ink"
        >
          <X className="size-3.5" aria-hidden />
        </button>
      ) : (
        <kbd className="absolute right-3 rounded-md border border-line px-1.5 py-0.5 text-[10px] text-ink-faint">
          /
        </kbd>
      )}
    </div>
  );
});
