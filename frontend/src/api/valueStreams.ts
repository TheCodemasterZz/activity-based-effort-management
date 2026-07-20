import { apiClient } from './client';
import type { PagedResult, ValueStreamDetailDto, ValueStreamDto } from './types';

export function getValueStreams(pageSize = 100) {
  return apiClient.get<PagedResult<ValueStreamDto>>('/api/v1/valuestreams', { pageSize });
}

export function getValueStreamById(id: string) {
  return apiClient.get<ValueStreamDetailDto>(`/api/v1/valuestreams/${id}`);
}
