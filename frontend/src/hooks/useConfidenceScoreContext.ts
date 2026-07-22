import { useMemo } from 'react';
import { useEmployeeWorkLogs } from './useWorkLogs';
import { useHolidays } from './useHolidays';
import { WORK_LOG_ENTRY_TYPE } from '../api/types';
import type { ConfidenceScoreContext } from '../lib/confidenceScore';

function dateKeyDaysAgo(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function todayKey(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Güven skoru motorunun ihtiyaç duyduğu bağlamı (aynı çalışanın son 90 gündeki Actual kayıtları +
 * resmi tatiller) toplar — ayarlardaki lookback pencereleri bunun İÇİNDE kalacak şekilde geniş
 * tutulur, asıl daraltma motorun kendisinde yapılır. `excludeWorkLogId` düzenleme modunda kaydın
 * kendisiyle karşılaştırılmasını önler. */
export function useConfidenceScoreContext(employeeId: string | null, excludeWorkLogId?: string): ConfidenceScoreContext {
  const logs = useEmployeeWorkLogs(employeeId, dateKeyDaysAgo(90), todayKey(), WORK_LOG_ENTRY_TYPE.Actual);
  const holidays = useHolidays();

  return useMemo(
    () => ({
      siblingLogs: (logs.data?.items ?? [])
        .filter((l) => l.id !== excludeWorkLogId)
        .map((l) => ({ id: l.id, workDate: l.workDate, hours: l.hours, description: l.description })),
      holidayDateKeys: new Set(holidays.data?.items.map((h) => h.date) ?? []),
    }),
    [logs.data, holidays.data, excludeWorkLogId],
  );
}
