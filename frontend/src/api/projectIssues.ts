import { apiClient } from './client';
import type { PagedResult, ProjectIssueDto, ProjectIssuePriorityValue, ProjectIssueStatusValue } from './types';

export function getProjectIssues(projectId: string) {
  return apiClient.get<PagedResult<ProjectIssueDto>>('/api/v1/projectissues', {
    projectId,
    pageSize: 200,
  });
}

/** Proje listesindeki Sorun RAG göstergesi için — tüm projelerin sorunlarını tek seferde çeker. */
export function getAllProjectIssues() {
  return apiClient.get<PagedResult<ProjectIssueDto>>('/api/v1/projectissues', { pageSize: 2000 });
}

export interface SaveProjectIssuePayload {
  projectId: string;
  title: string;
  description?: string | null;
  priority: ProjectIssuePriorityValue;
  ownerUserId?: string | null;
  dueDate?: string | null;
  resolution?: string | null;
}

export function createProjectIssue(payload: Omit<SaveProjectIssuePayload, 'resolution'>) {
  return apiClient.post<{ id: string }>('/api/v1/projectissues', payload);
}

export function updateProjectIssue(id: string, payload: Omit<SaveProjectIssuePayload, 'projectId'>) {
  return apiClient.put<void>(`/api/v1/projectissues/${id}`, payload);
}

export function updateProjectIssueStatus(id: string, status: ProjectIssueStatusValue) {
  return apiClient.put<void>(`/api/v1/projectissues/${id}/status`, { status });
}

export function deleteProjectIssue(id: string) {
  return apiClient.delete<void>(`/api/v1/projectissues/${id}`);
}
