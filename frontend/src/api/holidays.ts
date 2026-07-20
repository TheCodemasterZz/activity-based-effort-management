import { apiClient } from './client';
import type { PagedResult } from './types';

export interface HolidayDto {
  id: string;
  date: string;
  name: string;
}

export function getHolidays(year?: number) {
  return apiClient.get<PagedResult<HolidayDto>>('/api/v1/holidays', { year, pageSize: 100 });
}
