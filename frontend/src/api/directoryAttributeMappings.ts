import { apiClient } from './client';
import type { DirectoryAttributeMappingDto } from './types';

export interface SaveAttributeMappingPayload {
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export function getAttributeMappings() {
  return apiClient.get<DirectoryAttributeMappingDto[]>('/api/v1/directoryattributemappings');
}

export function createAttributeMapping(payload: SaveAttributeMappingPayload) {
  return apiClient.post<{ id: string }>('/api/v1/directoryattributemappings', payload);
}

export function updateAttributeMapping(id: string, payload: SaveAttributeMappingPayload) {
  return apiClient.put<void>(`/api/v1/directoryattributemappings/${id}`, { ...payload, id });
}

export function deleteAttributeMapping(id: string) {
  return apiClient.delete<void>(`/api/v1/directoryattributemappings/${id}`);
}
