import { useQuery } from '@tanstack/react-query';
import { getEmployeeLeaves, type GetEmployeeLeavesParams } from '../api/employeeLeaves';

export function useEmployeeLeaves(params?: GetEmployeeLeavesParams, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: ['employeeLeaves', params?.employeeId ?? null, params?.dateFrom ?? null, params?.dateTo ?? null],
    queryFn: () => getEmployeeLeaves(params),
    enabled: options?.enabled ?? true,
  });
}
