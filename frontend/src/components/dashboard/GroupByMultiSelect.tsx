import { useEffect, useRef, useState } from 'react';
import { GROUP_BY_OPTIONS, type GroupByDimension } from '../../lib/groupWorkLogs';

interface GroupByMultiSelectProps {
  value: GroupByDimension[];
  onChange: (value: GroupByDimension[]) => void;
}

export function GroupByMultiSelect({ value, onChange }: GroupByMultiSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
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

  const inactiveOptions = GROUP_BY_OPTIONS.filter((o) => !value.includes(o.value));

  const removeAt = (index: number) => {
    const next = value.filter((_, i) => i !== index);
    onChange(next.length > 0 ? next : [value[0]]);
  };

  const add = (dimension: GroupByDimension) => {
    onChange([...value, dimension]);
  };

  const moveTo = (from: number, to: number) => {
    if (from === to) return;
    const next = [...value];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);
    onChange(next);
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        className="flex w-[28rem] max-w-full items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs"
      >
        <span className="shrink-0 font-medium text-slate-500">Group by</span>
        <span className="flex min-w-0 flex-1 items-center gap-1 overflow-x-auto">
          {value.map((dim, index) => (
            <span
              key={dim}
              className="shrink-0 whitespace-nowrap rounded bg-slate-100 px-1.5 py-0.5 text-[11px] font-semibold text-slate-600"
            >
              {index + 1}. {GROUP_BY_OPTIONS.find((o) => o.value === dim)?.label}
            </span>
          ))}
        </span>
        <span className="shrink-0 text-slate-300">▾</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 z-40 mt-2 w-72 rounded-lg border border-slate-200 bg-white p-3 shadow-lg">
          <div className="mb-2 flex items-center justify-between text-sm font-semibold text-slate-700">
            <span>Group by</span>
            <span className="text-xs font-normal text-slate-400">Sıralamak için sürükle</span>
          </div>

          <div className="mb-3 space-y-1">
            {value.map((dim, index) => (
              <div
                key={dim}
                draggable
                onDragStart={() => setDragIndex(index)}
                onDragOver={(e) => e.preventDefault()}
                onDrop={(e) => {
                  e.preventDefault();
                  if (dragIndex !== null) moveTo(dragIndex, index);
                  setDragIndex(null);
                }}
                onDragEnd={() => setDragIndex(null)}
                className={
                  'flex cursor-grab items-center justify-between rounded px-2 py-1 text-sm text-indigo-700 hover:bg-indigo-50 active:cursor-grabbing' +
                  (dragIndex === index ? ' opacity-40' : '')
                }
              >
                <span className="flex items-center gap-1.5">
                  <span className="text-slate-300" aria-hidden="true">
                    ⠿
                  </span>
                  {index + 1}. {GROUP_BY_OPTIONS.find((o) => o.value === dim)?.label}
                </span>
                <button
                  type="button"
                  onClick={() => removeAt(index)}
                  className="text-slate-400 hover:text-red-600"
                  aria-label="Kaldır"
                >
                  ✕
                </button>
              </div>
            ))}
          </div>

          {inactiveOptions.length > 0 && (
            <>
              <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Inactive</div>
              <div className="space-y-1">
                {inactiveOptions.map((option) => (
                  <button
                    type="button"
                    key={option.value}
                    onClick={() => add(option.value)}
                    aria-label={`${option.label} ekle`}
                    className="flex w-full items-center justify-between rounded px-2 py-1 text-left text-sm text-slate-600 hover:bg-indigo-50 hover:text-indigo-700"
                  >
                    <span>{option.label}</span>
                    <span className="text-slate-400">+</span>
                  </button>
                ))}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}
