import { useRef, useState } from 'react';
import { formatRangeLabel } from '../../lib/dateUtils';
import { DateRangePickerModal, type DateRangeApplyResult } from './DateRangePickerModal';

interface MonthNavigatorProps {
  anchorDate: Date;
  startKey: string;
  endKey: string;
  onPrev: () => void;
  onNext: () => void;
  onApplyRange: (result: DateRangeApplyResult) => void;
}

export function MonthNavigator({ anchorDate, startKey, endKey, onPrev, onNext, onApplyRange }: MonthNavigatorProps) {
  const [isPickerOpen, setIsPickerOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <div className="relative flex items-center gap-2">
      <button
        type="button"
        onClick={onPrev}
        className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50"
        aria-label="Önceki dönem"
      >
        ‹
      </button>

      <button
        ref={triggerRef}
        type="button"
        onClick={() => setIsPickerOpen((v) => !v)}
        className="flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-1.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
      >
        <span aria-hidden="true">📅</span>
        {formatRangeLabel(startKey, endKey)}
      </button>

      {isPickerOpen && (
        <DateRangePickerModal
          anchorDate={anchorDate}
          anchorRef={triggerRef}
          onApply={onApplyRange}
          onClose={() => setIsPickerOpen(false)}
        />
      )}

      <button
        type="button"
        onClick={onNext}
        className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50"
        aria-label="Sonraki dönem"
      >
        ›
      </button>
    </div>
  );
}
