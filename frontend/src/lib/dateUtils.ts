import {
  addDays,
  addMonths,
  subMonths,
  startOfMonth,
  endOfMonth,
  eachDayOfInterval,
  addQuarters,
  subQuarters,
  startOfQuarter,
  endOfQuarter,
  eachWeekOfInterval,
  endOfWeek,
  addYears,
  subYears,
  startOfYear,
  endOfYear,
  eachMonthOfInterval,
  eachQuarterOfInterval,
  differenceInCalendarDays,
  format,
  getISOWeek,
  getQuarter,
} from 'date-fns';
import { tr } from 'date-fns/locale/tr';

export type PeriodMode = 'daily' | 'weekly' | 'monthly' | 'quarterly';

export interface PeriodColumn {
  key: string;
  label: string;
  sublabel?: string;
  startKey: string;
  endKey: string;
}

export interface PeriodRange {
  label: string;
  startKey: string;
  endKey: string;
  columns: PeriodColumn[];
}

export const dateKey = (d: Date) => format(d, 'yyyy-MM-dd');

export function getPeriodRange(mode: PeriodMode, anchor: Date): PeriodRange {
  if (mode === 'daily') {
    const start = startOfMonth(anchor);
    const end = endOfMonth(anchor);
    const columns: PeriodColumn[] = eachDayOfInterval({ start, end }).map((d) => ({
      key: dateKey(d),
      label: format(d, 'd'),
      sublabel: format(d, 'EEEEEE', { locale: tr }).toUpperCase(),
      startKey: dateKey(d),
      endKey: dateKey(d),
    }));
    return { label: format(start, 'MMMM yyyy', { locale: tr }), startKey: dateKey(start), endKey: dateKey(end), columns };
  }

  if (mode === 'weekly') {
    const start = startOfQuarter(anchor);
    const end = endOfQuarter(anchor);
    const weekStarts = eachWeekOfInterval({ start, end }, { weekStartsOn: 1 });
    const columns: PeriodColumn[] = weekStarts.map((ws) => {
      const we = endOfWeek(ws, { weekStartsOn: 1 });
      return {
        key: `${format(ws, 'yyyy')}-W${getISOWeek(ws)}`,
        label: `${getISOWeek(ws)}. Hafta`,
        sublabel: `${format(ws, 'd MMM', { locale: tr })}`,
        startKey: dateKey(ws),
        endKey: dateKey(we),
      };
    });
    const quarterNumber = Math.floor(start.getMonth() / 3) + 1;
    return { label: `${quarterNumber}. Çeyrek ${format(start, 'yyyy')}`, startKey: dateKey(start), endKey: dateKey(end), columns };
  }

  if (mode === 'monthly') {
    const start = startOfYear(anchor);
    const end = endOfYear(anchor);
    const columns: PeriodColumn[] = eachMonthOfInterval({ start, end }).map((m) => ({
      key: format(m, 'yyyy-MM'),
      label: format(m, 'MMM', { locale: tr }),
      startKey: dateKey(startOfMonth(m)),
      endKey: dateKey(endOfMonth(m)),
    }));
    return { label: format(start, 'yyyy'), startKey: dateKey(start), endKey: dateKey(end), columns };
  }

  // quarterly
  const start = startOfYear(anchor);
  const end = endOfYear(anchor);
  const columns: PeriodColumn[] = eachQuarterOfInterval({ start, end }).map((q) => {
    const qStart = startOfQuarter(q);
    const qEnd = endOfQuarter(q);
    return {
      key: `${format(qStart, 'yyyy')}-Q${getQuarter(qStart)}`,
      label: `${getQuarter(qStart)}. Çeyrek`,
      startKey: dateKey(qStart),
      endKey: dateKey(qEnd),
    };
  });
  return { label: format(start, 'yyyy'), startKey: dateKey(start), endKey: dateKey(end), columns };
}

export function navigatePeriod(mode: PeriodMode, anchor: Date, direction: 1 | -1): Date {
  if (mode === 'daily') return direction === 1 ? addMonths(anchor, 1) : subMonths(anchor, 1);
  if (mode === 'weekly') return direction === 1 ? addQuarters(anchor, 1) : subQuarters(anchor, 1);
  return direction === 1 ? addYears(anchor, 1) : subYears(anchor, 1);
}

/** Bugünün tarihi verilen sütunun [startKey, endKey] aralığına düşüyor mu (tüm modlarda geçerli). */
export function isCurrentColumn(column: PeriodColumn, todayKey: string): boolean {
  return todayKey >= column.startKey && todayKey <= column.endKey;
}

/** "01/Tem/26 - 31/Tem/26" biçiminde, Jira Tempo tarzı kısa aralık etiketi üretir. */
export function formatRangeLabel(startKey: string, endKey: string): string {
  const start = new Date(`${startKey}T00:00:00`);
  const end = new Date(`${endKey}T00:00:00`);
  const short = (d: Date) => format(d, 'dd/MMM/yy', { locale: tr });
  return `${short(start)} - ${short(end)}`;
}

export interface CustomRange {
  startKey: string;
  endKey: string;
}

/** Tarih aralığı seçiciden elle seçilen [from, to] aralığını — periyot moduna bakmaksızın —
 * birebir gün gün gösteren bir PeriodRange üretir (Jira Tempo'da özel aralık seçince olduğu gibi). */
export function buildCustomDailyRange(range: CustomRange): PeriodRange {
  const start = new Date(`${range.startKey}T00:00:00`);
  const end = new Date(`${range.endKey}T00:00:00`);
  const columns: PeriodColumn[] = eachDayOfInterval({ start, end }).map((d) => ({
    key: dateKey(d),
    label: format(d, 'd'),
    sublabel: format(d, 'EEEEEE', { locale: tr }).toUpperCase(),
    startKey: dateKey(d),
    endKey: dateKey(d),
  }));
  const label = `${format(start, 'd MMM', { locale: tr })} - ${format(end, 'd MMM yyyy', { locale: tr })}`;
  return { label, startKey: range.startKey, endKey: range.endKey, columns };
}

/** [startKey, endKey] aralığındaki her günün anahtarını (yyyy-MM-dd) döner. */
export function eachDateKeyInRange(startKey: string, endKey: string): string[] {
  const start = new Date(`${startKey}T00:00:00`);
  const end = new Date(`${endKey}T00:00:00`);
  return eachDayOfInterval({ start, end }).map(dateKey);
}

/** Özel aralığı kendi uzunluğu kadar ileri/geri kaydırır (ör. 3 günlük bir aralık, önceki/sonraki 3 güne geçer). */
export function shiftCustomRange(range: CustomRange, direction: 1 | -1): CustomRange {
  const start = new Date(`${range.startKey}T00:00:00`);
  const end = new Date(`${range.endKey}T00:00:00`);
  const spanDays = differenceInCalendarDays(end, start) + 1;
  const shiftDays = direction * spanDays;
  return {
    startKey: dateKey(addDays(start, shiftDays)),
    endKey: dateKey(addDays(end, shiftDays)),
  };
}
