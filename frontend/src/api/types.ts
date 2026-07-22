export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface CustomerDto {
  id: string;
  name: string;
}

export interface EmployeeDto {
  id: string;
  name: string;
  email: string | null;
  workCalendarId: string;
}

export interface ProjectDto {
  id: string;
  name: string;
  description: string | null;
  status: string;
  startDate: string | null;
  endDate: string | null;
  healthStatus: string;
}

// Backend'de global bir JsonStringEnumConverter yok — bu yüzden komut gövdelerinde (request)
// enum'lar WORK_LOG_ENTRY_TYPE'daki gibi SAYI olarak gönderilir; DTO'larda (response) ise
// ProjectMappingConfig'in .ToString() eşlemesi nedeniyle METİN olarak gelir (ör. "OnTrack").
export const PROJECT_HEALTH_STATUS = { OnTrack: 1, AtRisk: 2, NeedsHelp: 3 } as const;
export type ProjectHealthStatusValue = (typeof PROJECT_HEALTH_STATUS)[keyof typeof PROJECT_HEALTH_STATUS];

export const PROJECT_TASK_STATUS = { NotStarted: 1, InProgress: 2, Done: 3 } as const;
export type ProjectTaskStatusValue = (typeof PROJECT_TASK_STATUS)[keyof typeof PROJECT_TASK_STATUS];

export interface ProjectTaskDto {
  id: string;
  projectId: string;
  name: string;
  startDate: string;
  endDate: string;
  estimatedEffortHours: number;
  status: string;
  isMilestone: boolean;
  baselineEffortHours: number;
  baselineEndDate: string;
}

export interface CustomerSummaryDto {
  id: string;
  name: string;
}

export interface EmployeeSummaryDto {
  id: string;
  name: string;
}

export interface ProjectDetailDto {
  id: string;
  name: string;
  description: string | null;
  status: string;
  startDate: string | null;
  endDate: string | null;
  healthStatus: string;
  customers: CustomerSummaryDto[];
  employees: EmployeeSummaryDto[];
}

export interface ActivityDto {
  id: string;
  name: string;
  description: string | null;
  parentActivityId: string | null;
}

export interface ValueStreamStageDto {
  id: string;
  name: string;
  order: number;
}

export interface ValueStreamDto {
  id: string;
  name: string;
  description: string | null;
}

export interface ValueStreamDetailDto extends ValueStreamDto {
  stages: ValueStreamStageDto[];
}

// Backend'de global bir JsonStringEnumConverter yok — WorkLogEntryType JSON'da sayısal enum
// değeri olarak taşınır (WorkLogEntryType.cs: Actual=0, Planned=1). Log Work ekranı Actual,
// Plan Work ekranı Planned kayıtlarla çalışır — aynı tablo/API, sadece bu alanla ayrışıyorlar.
export const WORK_LOG_ENTRY_TYPE = { Actual: 0, Planned: 1 } as const;
export type WorkLogEntryType = (typeof WORK_LOG_ENTRY_TYPE)[keyof typeof WORK_LOG_ENTRY_TYPE];

export interface EmployeeWorkLogDto {
  id: string;
  employeeId: string;
  projectId: string;
  customerId: string;
  activityL1Id: string;
  activityL2Id: string;
  workDate: string;
  hours: number;
  description: string;
  isApproved: boolean;
  entryType: WorkLogEntryType;
}

// Backend'de global bir JsonStringEnumConverter yok — ApprovalPeriodType JSON'da sayısal enum
// değeri olarak taşınır. Onay yalnızca tam hafta bazında verilebildiği için tek geçerli değer 1 (Weekly).
export type ApprovalPeriodType = 1;

export interface WorkCalendarDayDto {
  dayOfWeek: number;
  isWorkingDay: boolean;
  startTime: string | null;
  endTime: string | null;
}

export interface WorkCalendarDetailDto {
  id: string;
  name: string;
  days: WorkCalendarDayDto[];
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
