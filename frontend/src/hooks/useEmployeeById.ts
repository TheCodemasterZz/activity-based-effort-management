import { useQuery } from '@tanstack/react-query';
import { getEmployeeById } from '../api/employees';

export function useEmployeeById(id: string | null) {
  return useQuery({
    queryKey: ['employees', id],
    queryFn: () => getEmployeeById(id as string),
    enabled: id !== null,
  });
}
