import { apiClient } from './client';
import type { CustomerDto, PagedResult } from './types';

export function getCustomers(options?: { nameFilter?: string; projectId?: string; pageSize?: number }) {
  return apiClient.get<PagedResult<CustomerDto>>('/api/v1/customers', {
    nameFilter: options?.nameFilter,
    projectId: options?.projectId,
    pageSize: options?.pageSize ?? 100,
  });
}
