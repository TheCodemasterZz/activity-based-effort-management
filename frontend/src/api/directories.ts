import { apiClient } from './client';
import type {
  DirectoryDto,
  DirectorySyncResultDto,
  LdapConnectionTestResult,
  PagedResult,
} from './types';

export interface SaveDirectoryPayload {
  name: string;
  source: number;
  directoryType?: string | null;
  hostname?: string | null;
  port: number;
  useSsl: boolean;
  bindUsername?: string | null;
  /** Boş bırakılırsa güncellemede mevcut şifre korunur. */
  bindPassword?: string | null;
  baseDn?: string | null;
  additionalUserDn?: string | null;
  additionalGroupDn?: string | null;
  permission: number;
  userObjectClass?: string | null;
  userObjectFilter?: string | null;
  usernameAttribute?: string | null;
  usernameRdnAttribute?: string | null;
  firstNameAttribute?: string | null;
  lastNameAttribute?: string | null;
  displayNameAttribute?: string | null;
  emailAttribute?: string | null;
  uniqueIdAttribute?: string | null;
  syncSchedule: number;
  sortOrder: number;
}

export function getDirectories() {
  return apiClient.get<PagedResult<DirectoryDto>>('/api/v1/directories', { pageSize: 100 });
}

export function getDirectoryById(id: string) {
  return apiClient.get<DirectoryDto>(`/api/v1/directories/${id}`);
}

export function createDirectory(payload: SaveDirectoryPayload) {
  return apiClient.post<void>('/api/v1/directories', payload);
}

export function updateDirectory(id: string, payload: SaveDirectoryPayload) {
  return apiClient.put<void>(`/api/v1/directories/${id}`, { ...payload, id });
}

export function deleteDirectory(id: string) {
  return apiClient.delete<void>(`/api/v1/directories/${id}`);
}

export function syncDirectory(id: string) {
  return apiClient.post<DirectorySyncResultDto>(`/api/v1/directories/${id}/sync`);
}

export function testDirectoryConnection(id: string) {
  return apiClient.post<LdapConnectionTestResult>(`/api/v1/directories/${id}/test-connection`);
}
