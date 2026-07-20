import { apiClient } from './client';
import type { WorkCalendarDetailDto } from './types';

export function getWorkCalendarById(id: string) {
  return apiClient.get<WorkCalendarDetailDto>(`/api/v1/workcalendars/${id}`);
}
