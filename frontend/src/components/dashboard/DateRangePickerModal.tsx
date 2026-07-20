import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import {
  addMonths,
  addQuarters,
  addWeeks,
  eachDayOfInterval,
  endOfMonth,
  endOfQuarter,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  startOfMonth,
  startOfQuarter,
  startOfWeek,
  subMonths,
  subQuarters,
  subWeeks,
} from 'date-fns';
import { tr } from 'date-fns/locale/tr';

const WEEKDAY_LABELS = ['Pt', 'Sa', 'Ça', 'Pe', 'Cu', 'Ct', 'Pz'];

type QuickUnit = 'week' | 'month' | 'quarter';

const QUICK_OPTIONS: { unit: QuickUnit; label: string }[] = [
  { unit: 'week', label: 'Hafta' },
  { unit: 'month', label: 'Ay' },
  { unit: 'quarter', label: 'Çeyrek' },
];

const OFFSET_TABS: { value: -1 | 0 | 1; label: string }[] = [
  { value: -1, label: 'Önceki' },
  { value: 0, label: 'Güncel' },
  { value: 1, label: 'Sonraki' },
];

export type DateRangeApplyResult =
  | { kind: 'quick'; anchor: Date }
  | { kind: 'custom'; from: Date; to: Date };

interface DateRangePickerModalProps {
  anchorDate: Date;
  anchorRef: React.RefObject<HTMLElement | null>;
  onApply: (result: DateRangeApplyResult) => void;
  onClose: () => void;
}

const POPUP_WIDTH_PX = 736; // Tailwind w-[46rem] @ 16px kök font boyutu

/** Tetikleyici butona göre viewport-relative (fixed) konum hesaplar — açılır panel sağa/sola
 * taşarsa veya ekranın altına yakınsa görünür alanda kalacak şekilde kırpar. */
function computePosition(trigger: HTMLElement): { top: number; left: number } {
  const rect = trigger.getBoundingClientRect();
  let left = rect.right - POPUP_WIDTH_PX;
  const maxLeft = window.innerWidth - POPUP_WIDTH_PX - 8;
  left = Math.min(Math.max(left, 8), Math.max(maxLeft, 8));
  return { top: rect.bottom + 8, left };
}

function buildMonthGrid(monthDate: Date): Date[] {
  const start = startOfWeek(startOfMonth(monthDate), { weekStartsOn: 1 });
  const end = endOfWeek(endOfMonth(monthDate), { weekStartsOn: 1 });
  return eachDayOfInterval({ start, end });
}

function dateKey(d: Date): string {
  return format(d, 'yyyy-MM-dd');
}

/** "Hafta/Ay/Çeyrek" + "Önceki/Güncel/Sonraki" kombinasyonundan, o birimin bugüne göre kaydırılmış
 * aralığını hesaplar — mevcut periyot modunu (Günlük/Haftalık/Aylık/3 Aylık) DEĞİŞTİRMEZ, sadece
 * tabloyu hangi tarihe göre göstereceğimizi (anchor) belirler; gösterim her zaman o an aktif olan
 * view'e göre olur. */
function computeQuickRange(unit: QuickUnit, offset: -1 | 0 | 1): { anchor: Date; from: Date; to: Date } {
  const today = new Date();

  let anchor: Date;
  if (unit === 'week') anchor = offset === 0 ? today : offset === 1 ? addWeeks(today, 1) : subWeeks(today, 1);
  else if (unit === 'month') anchor = offset === 0 ? today : offset === 1 ? addMonths(today, 1) : subMonths(today, 1);
  else anchor = offset === 0 ? today : offset === 1 ? addQuarters(today, 1) : subQuarters(today, 1);

  let from: Date;
  let to: Date;
  if (unit === 'week') {
    from = startOfWeek(anchor, { weekStartsOn: 1 });
    to = endOfWeek(anchor, { weekStartsOn: 1 });
  } else if (unit === 'month') {
    from = startOfMonth(anchor);
    to = endOfMonth(anchor);
  } else {
    from = startOfQuarter(anchor);
    to = endOfQuarter(anchor);
  }

  return { anchor, from, to };
}

export function DateRangePickerModal({ anchorDate, anchorRef, onApply, onClose }: DateRangePickerModalProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  const [from, setFrom] = useState(anchorDate);
  const [to, setTo] = useState(anchorDate);
  const [selectingSecond, setSelectingSecond] = useState(false);
  const [calendarMonth, setCalendarMonth] = useState(startOfMonth(anchorDate));
  const [quickUnit, setQuickUnit] = useState<QuickUnit | null>(null);
  const [offsetTab, setOffsetTab] = useState<-1 | 0 | 1>(0);
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null);

  // Ebeveyn içeriğin overflow:auto olması (ör. sayfanın kaydırılabilir ana alanı) absolute
  // konumlandırılmış bir açılır paneli kırpabiliyordu — "bazen görünmüyor" hatasının kaynağı
  // buydu. document.body'ye portal + viewport-relative (fixed) konum bu kırpmayı tamamen ortadan
  // kaldırır.
  useLayoutEffect(() => {
    if (anchorRef.current) setPosition(computePosition(anchorRef.current));
  }, [anchorRef]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      if (
        containerRef.current &&
        !containerRef.current.contains(target) &&
        !(anchorRef.current && anchorRef.current.contains(target))
      ) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const monthA = calendarMonth;
  const monthB = addMonths(calendarMonth, 1);
  const gridA = useMemo(() => buildMonthGrid(monthA), [monthA]);
  const gridB = useMemo(() => buildMonthGrid(monthB), [monthB]);

  const todayKey = dateKey(new Date());

  const applyQuickSelect = (unit: QuickUnit, offset: -1 | 0 | 1) => {
    const range = computeQuickRange(unit, offset);
    setQuickUnit(unit);
    setOffsetTab(offset);
    setFrom(range.from);
    setTo(range.to);
    setSelectingSecond(false);
    setCalendarMonth(startOfMonth(range.from));
  };

  const handleDayClick = (day: Date) => {
    setQuickUnit(null);
    if (!selectingSecond) {
      setFrom(day);
      setTo(day);
      setSelectingSecond(true);
    } else {
      if (day < from) {
        setTo(from);
        setFrom(day);
      } else {
        setTo(day);
      }
      setSelectingSecond(false);
    }
  };

  const handleFromInput = (value: string) => {
    if (!value) return;
    setQuickUnit(null);
    const next = new Date(`${value}T00:00:00`);
    setFrom(next);
    if (next > to) setTo(next);
    setCalendarMonth(startOfMonth(next));
  };

  const handleToInput = (value: string) => {
    if (!value) return;
    setQuickUnit(null);
    const next = new Date(`${value}T00:00:00`);
    setTo(next);
    if (next < from) setFrom(next);
  };

  const handleApply = () => {
    // Hızlı seçim (Hafta/Ay/Çeyrek): mevcut periyot modunu korur, sadece referans tarihi kaydırır.
    // Takvimden/From-To'dan elle seçim: seçilen [from, to] aralığı BİREBİR gün gün tabloya
    // yansıtılır (periyot modundan bağımsız) — böylece "3 gün seçtim ama ekrana gelmedi" durumu
    // oluşmaz, seçim her zaman doğrudan görünür olur.
    if (quickUnit) {
      onApply({ kind: 'quick', anchor: from });
    } else {
      onApply({ kind: 'custom', from, to });
    }
    onClose();
  };

  const renderMonth = (monthDate: Date, grid: Date[], showPrevArrow: boolean, showNextArrow: boolean) => (
    <div>
      <div className="mb-2 flex items-center justify-between">
        {showPrevArrow ? (
          <button
            type="button"
            onClick={() => setCalendarMonth((m) => addMonths(m, -1))}
            className="text-slate-400 hover:text-slate-600"
            aria-label="Önceki ay"
          >
            ‹
          </button>
        ) : (
          <span className="w-4" />
        )}
        <div className="text-sm font-semibold text-slate-700">{format(monthDate, 'MMMM yyyy', { locale: tr })}</div>
        {showNextArrow ? (
          <button
            type="button"
            onClick={() => setCalendarMonth((m) => addMonths(m, 1))}
            className="text-slate-400 hover:text-slate-600"
            aria-label="Sonraki ay"
          >
            ›
          </button>
        ) : (
          <span className="w-4" />
        )}
      </div>
      <div className="grid grid-cols-7 gap-y-1 text-center text-[11px]">
        {WEEKDAY_LABELS.map((d) => (
          <div key={d} className="font-semibold text-slate-400">
            {d}
          </div>
        ))}
        {grid.map((day) => {
          const inMonth = isSameMonth(day, monthDate);
          const inRange = day >= from && day <= to;
          const isFrom = isSameDay(day, from);
          const isTo = isSameDay(day, to);
          const isEndpoint = isFrom || isTo;
          const isToday = dateKey(day) === todayKey;

          return (
            <button
              type="button"
              key={day.toISOString()}
              onClick={() => handleDayClick(day)}
              className={
                'relative flex h-7 w-full items-center justify-center rounded-full text-xs ' +
                (!inMonth ? 'text-slate-300' : 'text-slate-700') +
                (inRange && !isEndpoint ? ' bg-indigo-50' : '') +
                (isEndpoint ? ' bg-indigo-600 font-semibold text-white' : ' hover:bg-indigo-50')
              }
            >
              {format(day, 'd')}
              {isToday && !isEndpoint && (
                <span className="absolute bottom-0.5 h-1 w-1 rounded-full bg-indigo-500" />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );

  if (!position) return null;

  return createPortal(
    <div
      ref={containerRef}
      style={{ position: 'fixed', top: position.top, left: position.left }}
      className="z-30 flex w-[46rem] max-w-[95vw] overflow-hidden rounded-xl border border-slate-200 bg-white shadow-2xl"
    >
      <div className="flex-1 p-4">
        <div className="mb-4 grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">From:</label>
            <input
              type="date"
              value={dateKey(from)}
              onChange={(e) => handleFromInput(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-500">To:</label>
            <input
              type="date"
              value={dateKey(to)}
              onChange={(e) => handleToInput(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-2.5 py-1.5 text-sm"
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-6">
          {renderMonth(monthA, gridA, true, false)}
          {renderMonth(monthB, gridB, false, true)}
        </div>
      </div>

      <div className="w-48 shrink-0 border-l border-slate-200 bg-slate-50 p-4">
        <div className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-400">Hızlı Tarih Seç</div>

        <div className="mb-3 flex rounded-lg border border-slate-200 bg-white p-0.5 text-xs">
          {OFFSET_TABS.map((tab) => (
            <button
              key={tab.value}
              type="button"
              onClick={() => (quickUnit ? applyQuickSelect(quickUnit, tab.value) : setOffsetTab(tab.value))}
              className={
                'flex-1 rounded-md px-1.5 py-1 font-medium ' +
                (offsetTab === tab.value ? 'bg-indigo-600 text-white' : 'text-slate-500 hover:text-slate-700')
              }
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="flex flex-col gap-1">
          {QUICK_OPTIONS.map((option) => (
            <button
              key={option.unit}
              type="button"
              onClick={() => applyQuickSelect(option.unit, offsetTab)}
              className={
                'rounded-md px-2.5 py-1.5 text-left text-sm ' +
                (quickUnit === option.unit
                  ? 'bg-indigo-100 font-semibold text-indigo-700'
                  : 'text-slate-600 hover:bg-slate-100')
              }
            >
              {option.label}
            </button>
          ))}
        </div>

        <div className="mt-6 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-100"
          >
            Vazgeç
          </button>
          <button
            type="button"
            onClick={handleApply}
            className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
          >
            Uygula
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
