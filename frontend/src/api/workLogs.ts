import { apiClient } from './client';
import { WORK_LOG_ENTRY_TYPE, type EmployeeWorkLogDto, type PagedResult, type WorkLogEntryType } from './types';

export interface GetWorkLogsParams {
  dateFrom?: string;
  dateTo?: string;
  employeeId?: string;
  projectId?: string;
  pageNumber?: number;
  pageSize?: number;
  entryType?: WorkLogEntryType;
}

export function getWorkLogs(params: GetWorkLogsParams) {
  return apiClient.get<PagedResult<EmployeeWorkLogDto>>('/api/v1/employeeworklogs', {
    dateFrom: params.dateFrom,
    dateTo: params.dateTo,
    employeeId: params.employeeId,
    projectId: params.projectId,
    pageNumber: params.pageNumber ?? 1,
    // Backend'in PaginationParams.MaxPageSize'ı (bkz. PaginationParams.cs) 5000 — sayfasız "tüm
    // dönemi çek" istekleri bu üst sınırla eşleşmeli, aksi halde bir dönemdeki kayıt sayısı bu
    // değeri aşınca (ör. çok-parçalı mock veri sonrası bir ayda 3000+ kayıt) sonuç sessizce
    // kırpılır ve en son eklenen kayıtlar tabloda hiç görünmez.
    pageSize: params.pageSize ?? 5000,
    entryType: params.entryType ?? WORK_LOG_ENTRY_TYPE.Actual,
  });
}

export interface LogWorkPayload {
  employeeId: string;
  projectId: string;
  customerId: string;
  activityL1Id: string;
  activityL2Id: string;
  startDate: string;
  endDate: string;
  hours: number;
  description: string;
  entryType?: WorkLogEntryType;
}

export function logWork(payload: LogWorkPayload) {
  return apiClient.post<{ ids: string[] }>('/api/v1/employeeworklogs', payload);
}

export interface UpdateWorkLogPayload {
  employeeId: string;
  projectId: string;
  customerId: string;
  activityL1Id: string;
  activityL2Id: string;
  workDate: string;
  hours: number;
  description: string;
}

export function updateWorkLog(id: string, payload: UpdateWorkLogPayload) {
  return apiClient.put<void>(`/api/v1/employeeworklogs/${id}`, payload);
}

export function deleteWorkLog(id: string) {
  return apiClient.delete<void>(`/api/v1/employeeworklogs/${id}`);
}
