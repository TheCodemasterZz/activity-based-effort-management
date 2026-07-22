import { apiClient } from './client';
import { WORK_LOG_ENTRY_TYPE, type ApprovalPeriodType, type PagedResult, type WorkLogEntryType } from './types';

export interface CreateWorkLogApprovalPayload {
  employeeId: string;
  periodType: ApprovalPeriodType;
  periodStart: string;
  periodEnd: string;
  description?: string | null;
  entryType?: WorkLogEntryType;
}

export function createWorkLogApproval(payload: CreateWorkLogApprovalPayload) {
  return apiClient.post<{ id: string }>('/api/v1/workLogApprovals', payload);
}

export interface WorkLogApprovalDto {
  id: string;
  employeeId: string;
  periodStart: string;
  periodEnd: string;
  description?: string | null;
  entryType: WorkLogEntryType;
}

export function getWorkLogApprovals(entryType: WorkLogEntryType = WORK_LOG_ENTRY_TYPE.Actual) {
  return apiClient.get<PagedResult<WorkLogApprovalDto>>('/api/v1/workLogApprovals', { pageSize: 100, entryType });
}
