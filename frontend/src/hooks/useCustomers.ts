import { useQuery } from '@tanstack/react-query';
import { getCustomers } from '../api/customers';

export function useCustomers() {
  return useQuery({ queryKey: ['customers'], queryFn: () => getCustomers() });
}

/** Yazdıkça arama — seçili projeye atanmış müşteriler arasından, küçük sayfa boyutuyla sunucu taraflı arar. */
export function useCustomerSearch(nameFilter: string, projectId: string | null) {
  return useQuery({
    queryKey: ['customers', 'search', nameFilter, projectId],
    queryFn: () => getCustomers({ nameFilter, projectId: projectId ?? undefined, pageSize: 10 }),
    enabled: projectId !== null,
  });
}
