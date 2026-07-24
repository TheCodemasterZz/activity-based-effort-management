import { apiClient } from './client';
import type { UserDetailDto, UserDto, PagedResult } from './types';

export function getUsers(options?: {
  directoryId?: string;
  searchTerm?: string;
  onlyActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}) {
  return apiClient.get<PagedResult<UserDto>>('/api/v1/users', {
    directoryId: options?.directoryId,
    searchTerm: options?.searchTerm,
    onlyActive: options?.onlyActive,
    pageNumber: options?.pageNumber ?? 1,
    pageSize: options?.pageSize ?? 25,
  });
}

export function getUserById(id: string) {
  return apiClient.get<UserDetailDto>(`/api/v1/users/${id}`);
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
  return apiClient.post<void>('/api/v1/users/internal', payload);
}

export function resetInternalUserPassword(userId: string, newPassword: string) {
  return apiClient.post<void>(`/api/v1/users/${userId}/reset-password`, {
    userId,
    newPassword,
  });
}
