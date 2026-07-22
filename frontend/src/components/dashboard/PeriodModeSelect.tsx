import { useEffect, useRef, useState } from 'react';
import type { PeriodMode } from '../../lib/dateUtils';

const OPTIONS: { mode: PeriodMode; label: string }[] = [
  { mode: 'daily', label: 'Günlük' },
  { mode: 'weekly', label: 'Haftalık' },
  { mode: 'monthly', label: 'Aylık' },
  { mode: 'quarterly', label: '3 Aylık' },
];

interface PeriodModeSelectProps {
  value: PeriodMode;
  onChange: (mode: PeriodMode) => void;
}

/** Günlük/Haftalık/Aylık/3 Aylık görünüm seçimi — dört ayrı sekme yerine tek bir tuşla,
 * alandan kazanmak için tek-seçimli açılır liste olarak tasarlanmıştır. */
export function PeriodModeSelect({ value, onChange }: PeriodModeSelectProps) {
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

  const currentLabel = OPTIONS.find((o) => o.mode === value)?.label ?? '';

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        className="flex items-center gap-2 rounded-lg bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white shadow hover:bg-indigo-700"
      >
        {currentLabel}
        <span className="text-indigo-200">▾</span>
      </button>

      {isOpen && (
        <div className="absolute left-0 top-full z-40 mt-2 w-36 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">
          {OPTIONS.map((option) => (
            <button
              key={option.mode}
              type="button"
              onClick={() => {
                onChange(option.mode);
                setIsOpen(false);
              }}
              className={
                'block w-full rounded-md px-3 py-1.5 text-left text-sm ' +
                (option.mode === value
                  ? 'bg-indigo-50 font-semibold text-indigo-700'
                  : 'text-slate-600 hover:bg-slate-50')
              }
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
