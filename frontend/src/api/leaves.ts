import { apiClient } from './client';
import type { PagedResult } from './types';

export interface LeaveDto {
  id: string;
  userId: string;
  startDate: string;
  endDate: string;
  isFullDay: boolean;
  startTime: string | null;
  endTime: string | null;
  description: string | null;
}

export interface GetLeavesParams {
  userId?: string;
  dateFrom?: string;
  dateTo?: string;
  pageSize?: number;
}

export function getLeaves(params?: GetLeavesParams) {
  return apiClient.get<PagedResult<LeaveDto>>('/api/v1/leaves', {
    userId: params?.userId,
    dateFrom: params?.dateFrom,
    dateTo: params?.dateTo,
    pageSize: params?.pageSize ?? 100,
  });
}

export interface CreateLeavePayload {
  userId: string;
  startDate: string;
  endDate: string;
  isFullDay: boolean;
  startTime: string | null;
  endTime: string | null;
  description?: string | null;
}

export function createLeave(payload: CreateLeavePayload) {
  return apiClient.post<{ id: string }>('/api/v1/leaves', payload);
}

export function deleteLeave(id: string) {
  return apiClient.delete<void>(`/api/v1/leaves/${id}`);
}
