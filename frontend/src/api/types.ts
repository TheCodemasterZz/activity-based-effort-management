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
