import { useQuery } from '@tanstack/react-query';
import { getWorkLogs } from '../api/workLogs';

export function useWorkLogs(dateFrom: string, dateTo: string) {
  return useQuery({
    queryKey: ['workLogs', dateFrom, dateTo],
    queryFn: () => getWorkLogs({ dateFrom, dateTo }),
  });
}

/** Tek bir çalışanın belirli bir tarih aralığındaki kayıtları — onay modalındaki önizleme için. */
export function useEmployeeWorkLogs(employeeId: string | null, dateFrom: string, dateTo: string) {
  return useQuery({
    queryKey: ['workLogs', 'byEmployee', employeeId, dateFrom, dateTo],
    queryFn: () => getWorkLogs({ employeeId: employeeId as string, dateFrom, dateTo, pageSize: 100 }),
    enabled: employeeId !== null,
  });
}
