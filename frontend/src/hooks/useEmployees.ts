import { useQuery } from '@tanstack/react-query';
import { getEmployees } from '../api/employees';

export function useEmployees() {
  return useQuery({ queryKey: ['employees'], queryFn: () => getEmployees() });
}

/** Yazdıkça arama — küçük sayfa boyutuyla sunucu taraflı arar. */
export function useEmployeeSearch(nameFilter: string) {
  return useQuery({
    queryKey: ['employees', 'search', nameFilter],
    queryFn: () => getEmployees({ nameFilter, pageSize: 10 }),
  });
}
