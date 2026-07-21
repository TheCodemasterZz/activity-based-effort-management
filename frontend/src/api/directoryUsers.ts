import { apiClient } from './client';
import type { DirectoryUserDetailDto, DirectoryUserDto, PagedResult } from './types';

export function getDirectoryUsers(options?: {
  directoryId?: string;
  searchTerm?: string;
  onlyActive?: boolean;
  pageSize?: number;
}) {
  return apiClient.get<PagedResult<DirectoryUserDto>>('/api/v1/directoryusers', {
    directoryId: options?.directoryId,
    searchTerm: options?.searchTerm,
    onlyActive: options?.onlyActive,
    pageSize: options?.pageSize ?? 100,
  });
}

export function getDirectoryUserById(id: string) {
  return apiClient.get<DirectoryUserDetailDto>(`/api/v1/directoryusers/${id}`);
}

export interface CreateInternalUserPayload {
  directoryId: string;
  username: string;
  password: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  email?: string | null;
}

export function createInternalUser(payload: CreateInternalUserPayload) {
  return apiClient.post<void>('/api/v1/directoryusers/internal', payload);
}
