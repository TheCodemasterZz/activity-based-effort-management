import { apiClient } from './client';
import type { PagedResult, WorkCalendarDetailDto, WorkCalendarDto } from './types';

export function getWorkCalendarById(id: string) {
  return apiClient.get<WorkCalendarDetailDto>(`/api/v1/workcalendars/${id}`);
}

export function getWorkCalendars(pageSize = 100) {
  return apiClient.get<PagedResult<WorkCalendarDto>>('/api/v1/workcalendars', { pageSize });
}
