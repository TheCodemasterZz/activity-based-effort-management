import { apiClient } from './client';
import type { PagedResult, ProjectRiskDto, ProjectRiskStatusValue } from './types';

export function getProjectRisks(projectId: string) {
  return apiClient.get<PagedResult<ProjectRiskDto>>('/api/v1/projectrisks', {
    projectId,
    pageSize: 200,
  });
}

/** Proje listesindeki Risk RAG göstergesi için — tüm projelerin risklerini tek seferde çeker
 * (bkz. getAllProjectTasks'taki aynı N+1-önleme deseni). */
export function getAllProjectRisks() {
  return apiClient.get<PagedResult<ProjectRiskDto>>('/api/v1/projectrisks', { pageSize: 2000 });
}

export interface SaveProjectRiskPayload {
  projectId: string;
  title: string;
  description?: string | null;
  probability: number;
  impact: number;
  mitigationPlan?: string | null;
  ownerEmployeeId?: string | null;
  identifiedDate: string;
}

export function createProjectRisk(payload: SaveProjectRiskPayload) {
  return apiClient.post<{ id: string }>('/api/v1/projectrisks', payload);
}

export function updateProjectRisk(id: string, payload: Omit<SaveProjectRiskPayload, 'projectId'>) {
  return apiClient.put<void>(`/api/v1/projectrisks/${id}`, payload);
}

export function updateProjectRiskStatus(id: string, status: ProjectRiskStatusValue) {
  return apiClient.put<void>(`/api/v1/projectrisks/${id}/status`, { status });
}

export function deleteProjectRisk(id: string) {
  return apiClient.delete<void>(`/api/v1/projectrisks/${id}`);
}
