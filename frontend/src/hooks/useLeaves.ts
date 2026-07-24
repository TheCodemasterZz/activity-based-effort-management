import { useQuery } from '@tanstack/react-query';
import { getLeaves, type GetLeavesParams } from '../api/leaves';

export function useLeaves(params?: GetLeavesParams, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: ['leaves', params?.userId ?? null, params?.dateFrom ?? null, params?.dateTo ?? null],
    queryFn: () => getLeaves(params),
    enabled: options?.enabled ?? true,
  });
}
