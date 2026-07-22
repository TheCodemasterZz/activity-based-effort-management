import { apiClient } from './client';
import type { DirectoryAttributeMappingDto } from './types';

export interface SaveAttributeMappingPayload {
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export function getAttributeMappings(directoryId: string) {
  return apiClient.get<DirectoryAttributeMappingDto[]>(
    `/api/v1/directories/${directoryId}/attribute-mappings`,
  );
}

export function createAttributeMapping(directoryId: string, payload: SaveAttributeMappingPayload) {
  return apiClient.post<{ id: string }>(
    `/api/v1/directories/${directoryId}/attribute-mappings`,
    payload,
  );
}

export function updateAttributeMapping(
  directoryId: string,
  id: string,
  payload: SaveAttributeMappingPayload,
) {
  return apiClient.put<void>(
    `/api/v1/directories/${directoryId}/attribute-mappings/${id}`,
    { ...payload, id },
  );
}

export function deleteAttributeMapping(directoryId: string, id: string) {
  return apiClient.delete<void>(`/api/v1/directories/${directoryId}/attribute-mappings/${id}`);
}
