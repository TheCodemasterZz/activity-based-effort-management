import { apiClient } from './client';
import type { PagedResult, ProjectTaskDto, ProjectTaskStatusValue } from './types';

export function getProjectTasks(projectId: string) {
  return apiClient.get<PagedResult<ProjectTaskDto>>('/api/v1/projecttasks', {
    projectId,
    pageSize: 200,
  });
}

/** Kart-grid görünümünde (bkz. ProjectCard) proje başına ayrı bir istek atmamak için, tüm
 * projelerin görevleri tek seferde çekilip client tarafında projectId'ye göre gruplanır. */
export function getAllProjectTasks() {
  return apiClient.get<PagedResult<ProjectTaskDto>>('/api/v1/projecttasks', { pageSize: 2000 });
}

export interface SaveProjectTaskPayload {
  projectId: string;
  name: string;
  startDate: string;
  endDate: string;
  estimatedEffortHours: number;
  isMilestone: boolean;
  parentTaskId?: string | null;
  dependsOnTaskId?: string | null;
  assignedEmployeeId?: string | null;
}

export function createProjectTask(payload: SaveProjectTaskPayload) {
  return apiClient.post<{ id: string }>('/api/v1/projecttasks', payload);
}

export function updateProjectTask(id: string, payload: Omit<SaveProjectTaskPayload, 'projectId'>) {
  return apiClient.put<void>(`/api/v1/projecttasks/${id}`, payload);
}

export function updateProjectTaskStatus(id: string, status: ProjectTaskStatusValue) {
  return apiClient.put<void>(`/api/v1/projecttasks/${id}/status`, { status });
}

export function deleteProjectTask(id: string) {
  return apiClient.delete<void>(`/api/v1/projecttasks/${id}`);
}
