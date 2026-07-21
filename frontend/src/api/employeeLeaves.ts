import { apiClient } from './client';
import type { PagedResult } from './types';

export interface EmployeeLeaveDto {
  id: string;
  employeeId: string;
  startDate: string;
  endDate: string;
  isFullDay: boolean;
  startTime: string | null;
  endTime: string | null;
  description: string | null;
}

export interface GetEmployeeLeavesParams {
  employeeId?: string;
  dateFrom?: string;
  dateTo?: string;
  pageSize?: number;
}

export function getEmployeeLeaves(params?: GetEmployeeLeavesParams) {
  return apiClient.get<PagedResult<EmployeeLeaveDto>>('/api/v1/employeeleaves', {
    employeeId: params?.employeeId,
    dateFrom: params?.dateFrom,
    dateTo: params?.dateTo,
    pageSize: params?.pageSize ?? 100,
  });
}

export interface CreateEmployeeLeavePayload {
  employeeId: string;
  startDate: string;
  endDate: string;
  isFullDay: boolean;
  startTime: string | null;
  endTime: string | null;
  description?: string | null;
}

export function createEmployeeLeave(payload: CreateEmployeeLeavePayload) {
  return apiClient.post<{ id: string }>('/api/v1/employeeleaves', payload);
}

export function deleteEmployeeLeave(id: string) {
  return apiClient.delete<void>(`/api/v1/employeeleaves/${id}`);
}
