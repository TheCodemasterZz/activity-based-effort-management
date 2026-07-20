import { apiClient } from './client';
import type { EmployeeDto, PagedResult } from './types';

export function getEmployees(options?: { nameFilter?: string; pageSize?: number }) {
  return apiClient.get<PagedResult<EmployeeDto>>('/api/v1/employees', {
    nameFilter: options?.nameFilter,
    pageSize: options?.pageSize ?? 100,
  });
}

export function getEmployeeById(id: string) {
  return apiClient.get<EmployeeDto>(`/api/v1/employees/${id}`);
}
