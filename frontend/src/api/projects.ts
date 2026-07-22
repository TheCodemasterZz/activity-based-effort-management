import { apiClient } from './client';
import type { PagedResult, ProjectDetailDto, ProjectDto, ProjectHealthStatusValue } from './types';

export function getProjects(options?: { nameFilter?: string; employeeId?: string; pageSize?: number }) {
  return apiClient.get<PagedResult<ProjectDto>>('/api/v1/projects', {
    nameFilter: options?.nameFilter,
    employeeId: options?.employeeId,
    pageSize: options?.pageSize ?? 100,
  });
}

export function getProjectById(id: string) {
  return apiClient.get<ProjectDetailDto>(`/api/v1/projects/${id}`);
}

export interface SaveProjectPayload {
  name: string;
  description?: string | null;
  startDate?: string | null;
  endDate?: string | null;
}

export function createProject(payload: SaveProjectPayload) {
  return apiClient.post<void>('/api/v1/projects', payload);
}

export function updateProject(id: string, payload: SaveProjectPayload) {
  return apiClient.put<void>(`/api/v1/projects/${id}`, payload);
}

export function deleteProject(id: string) {
  return apiClient.delete<void>(`/api/v1/projects/${id}`);
}

export function updateProjectHealth(id: string, healthStatus: ProjectHealthStatusValue) {
  return apiClient.put<void>(`/api/v1/projects/${id}/health`, { healthStatus });
}
