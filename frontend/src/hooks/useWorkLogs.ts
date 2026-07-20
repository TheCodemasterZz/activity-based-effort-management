import { useQuery } from '@tanstack/react-query';
import { getWorkLogs } from '../api/workLogs';

export function useWorkLogs(dateFrom: string, dateTo: string) {
  return useQuery({
    queryKey: ['workLogs', dateFrom, dateTo],
    queryFn: () => getWorkLogs({ dateFrom, dateTo }),
  });
}
