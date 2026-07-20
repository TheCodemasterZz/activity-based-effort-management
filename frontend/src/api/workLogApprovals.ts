import { apiClient } from './client';
import type { ApprovalPeriodType, PagedResult } from './types';

export interface CreateWorkLogApprovalPayload {
  employeeId: string;
  periodType: ApprovalPeriodType;
  periodStart: string;
  periodEnd: string;
}

export function createWorkLogApproval(payload: CreateWorkLogApprovalPayload) {
  return apiClient.post<{ id: string }>('/api/v1/workLogApprovals', payload);
}

export interface WorkLogApprovalDto {
  id: string;
  employeeId: string;
  periodStart: string;
  periodEnd: string;
}

export function getWorkLogApprovals() {
  return apiClient.get<PagedResult<WorkLogApprovalDto>>('/api/v1/workLogApprovals', { pageSize: 100 });
}
