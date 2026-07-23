import { apiClient } from './client';
import type { RoleDetailDto, RoleDto } from './types';

export interface SaveRolePayload {
  name: string;
  description: string | null;
}

export function getRoles() {
  return apiClient.get<RoleDto[]>('/api/v1/roles');
}

export function getRoleById(id: string) {
  return apiClient.get<RoleDetailDto>(`/api/v1/roles/${id}`);
}

export function getPermissionCatalog() {
  return apiClient.get<string[]>('/api/v1/roles/permission-catalog');
}

export function createRole(payload: SaveRolePayload) {
  return apiClient.post<{ id: string }>('/api/v1/roles', payload);
}

export function updateRole(id: string, payload: SaveRolePayload) {
  return apiClient.put<void>(`/api/v1/roles/${id}`, payload);
}

export function deleteRole(id: string) {
  return apiClient.delete<void>(`/api/v1/roles/${id}`);
}

export function grantPermission(roleId: string, permissionKey: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/permissions`, { permissionKey });
}

export function revokePermission(roleId: string, permissionKey: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/permissions/revoke`, { permissionKey });
}

export function assignUserToRole(roleId: string, userId: string) {
  return apiClient.post<void>(`/api/v1/roles/${roleId}/users`, { userId });
}

export function removeUserFromRole(roleId: string, userId: string) {
  return apiClient.delete<void>(`/api/v1/roles/${roleId}/users/${userId}`);
}
