import { useQuery } from '@tanstack/react-query';
import { getWorkLogs } from '../api/workLogs';
import { WORK_LOG_ENTRY_TYPE, type WorkLogEntryType } from '../api/types';

export function useWorkLogs(dateFrom: string, dateTo: string, entryType: WorkLogEntryType = WORK_LOG_ENTRY_TYPE.Actual) {
  return useQuery({
    queryKey: ['workLogs', dateFrom, dateTo, entryType],
    queryFn: () => getWorkLogs({ dateFrom, dateTo, entryType }),
  });
}

/** Tek bir çalışanın belirli bir tarih aralığındaki kayıtları — onay modalındaki önizleme için. */
export function useEmployeeWorkLogs(
  employeeId: string | null,
  dateFrom: string,
  dateTo: string,
  entryType: WorkLogEntryType = WORK_LOG_ENTRY_TYPE.Actual,
) {
  return useQuery({
    queryKey: ['workLogs', 'byEmployee', employeeId, dateFrom, dateTo, entryType],
    queryFn: () => getWorkLogs({ employeeId: employeeId as string, dateFrom, dateTo, pageSize: 100, entryType }),
    enabled: employeeId !== null,
  });
}
