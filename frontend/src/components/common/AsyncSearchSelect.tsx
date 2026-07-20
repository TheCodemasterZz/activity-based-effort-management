import { useEffect, useRef, useState } from 'react';

export interface AsyncSearchOption {
  id: string;
  label: string;
}

interface AsyncSearchSelectProps {
  selectedLabel: string | null;
  onSearch: (query: string) => void;
  options: AsyncSearchOption[];
  isLoading: boolean;
  onSelect: (option: AsyncSearchOption) => void;
  placeholder: string;
  disabled?: boolean;
  disabledMessage?: string;
}

/**
 * Yazdıkça arayan (debounced), sunucudan küçük bir sayfa halinde sonuç getiren
 * arama kutusu — tüm listeyi tek seferde çekip ekrana basmaz.
 */
export function AsyncSearchSelect({
  selectedLabel,
  onSearch,
  options,
  isLoading,
  onSelect,
  placeholder,
  disabled,
  disabledMessage,
}: AsyncSearchSelectProps) {
  const [query, setQuery] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    const timer = setTimeout(() => onSearch(query), 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, isOpen]);

  return (
    <div ref={containerRef} className="relative">
      <input
        type="text"
        value={isOpen ? query : (selectedLabel ?? '')}
        onFocus={() => {
          setIsOpen(true);
          setQuery('');
          onSearch('');
        }}
        onChange={(e) => setQuery(e.target.value)}
        placeholder={disabled ? (disabledMessage ?? placeholder) : placeholder}
        disabled={disabled}
        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm disabled:bg-slate-50"
      />
      {isOpen && !disabled && (
        <div className="absolute z-20 mt-1 max-h-48 w-full overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-lg">
          {isLoading && <div className="px-3 py-2 text-sm text-slate-400">Aranıyor…</div>}
          {!isLoading && options.length === 0 && (
            <div className="px-3 py-2 text-sm text-slate-400">Sonuç yok</div>
          )}
          {options.map((option) => (
            <button
              key={option.id}
              type="button"
              onClick={() => {
                onSelect(option);
                setIsOpen(false);
                setQuery('');
              }}
              className="block w-full px-3 py-2 text-left text-sm hover:bg-indigo-50"
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
