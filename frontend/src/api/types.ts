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
  sponsor: string | null;
  projectManagerEmployeeId: string | null;
  priority: string;
  strategicGoal: string | null;
}

// Backend'de global bir JsonStringEnumConverter yok — bu yüzden komut gövdelerinde (request)
// enum'lar WORK_LOG_ENTRY_TYPE'daki gibi SAYI olarak gönderilir; DTO'larda (response) ise
// ProjectMappingConfig'in .ToString() eşlemesi nedeniyle METİN olarak gelir (ör. "OnTrack").
export const PROJECT_HEALTH_STATUS = { OnTrack: 1, AtRisk: 2, NeedsHelp: 3 } as const;
export type ProjectHealthStatusValue = (typeof PROJECT_HEALTH_STATUS)[keyof typeof PROJECT_HEALTH_STATUS];

export const PROJECT_PRIORITY = { Low: 1, Medium: 2, High: 3, Critical: 4 } as const;
export type ProjectPriorityValue = (typeof PROJECT_PRIORITY)[keyof typeof PROJECT_PRIORITY];

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
  parentTaskId: string | null;
  dependsOnTaskId: string | null;
  assignedEmployeeId: string | null;
}

export const PROJECT_RISK_STATUS = { Open: 1, Mitigating: 2, Closed: 3 } as const;
export type ProjectRiskStatusValue = (typeof PROJECT_RISK_STATUS)[keyof typeof PROJECT_RISK_STATUS];

export interface ProjectRiskDto {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  probability: number;
  impact: number;
  status: string;
  mitigationPlan: string | null;
  ownerEmployeeId: string | null;
  identifiedDate: string;
}

export const PROJECT_ISSUE_PRIORITY = { Low: 1, Medium: 2, High: 3, Critical: 4 } as const;
export type ProjectIssuePriorityValue = (typeof PROJECT_ISSUE_PRIORITY)[keyof typeof PROJECT_ISSUE_PRIORITY];

export const PROJECT_ISSUE_STATUS = { Open: 1, InProgress: 2, Resolved: 3, Closed: 4 } as const;
export type ProjectIssueStatusValue = (typeof PROJECT_ISSUE_STATUS)[keyof typeof PROJECT_ISSUE_STATUS];

export interface ProjectIssueDto {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  priority: string;
  status: string;
  ownerEmployeeId: string | null;
  dueDate: string | null;
  resolution: string | null;
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
  sponsor: string | null;
  projectManagerEmployeeId: string | null;
  priority: string;
  strategicGoal: string | null;
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

export interface LoginResultDto {
  token: string;
  expiresAtUtc: string;
  userId: string;
  username: string;
  displayName: string | null;
  source: number;
}

export interface DirectoryDto {
  id: string;
  name: string;
  source: number;
  directoryType: string | null;
  hostname: string | null;
  port: number;
  useSsl: boolean;
  bindUsername: string | null;
  baseDn: string | null;
  additionalUserDn: string | null;
  additionalGroupDn: string | null;
  permission: number;
  userObjectClass: string | null;
  userObjectFilter: string | null;
  usernameAttribute: string | null;
  usernameRdnAttribute: string | null;
  firstNameAttribute: string | null;
  lastNameAttribute: string | null;
  displayNameAttribute: string | null;
  emailAttribute: string | null;
  uniqueIdAttribute: string | null;
  syncSchedule: number;
  isActive: boolean;
  sortOrder: number;
  lastSyncedUtc: string | null;
}

export interface DirectoryUserDto {
  id: string;
  directoryId: string;
  directoryName: string;
  source: number;
  username: string;
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  email: string | null;
  isActive: boolean;
  lastSyncedUtc: string | null;
}

export interface DirectoryUserAttributeValueDto {
  systemFieldName: string;
  adAttributeName: string;
  fieldType: string;
  value: string | null;
  referencedDirectoryUserId: string | null;
}

export interface DirectoryUserDetailDto extends DirectoryUserDto {
  attributes: DirectoryUserAttributeValueDto[];
}

export interface DirectoryAttributeMappingDto {
  id: string;
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export interface DirectorySyncResultDto {
  directoryId: string;
  directoryName: string;
  added: number;
  updated: number;
  deactivated: number;
  totalFromDirectory: number;
  syncedAtUtc: string;
}

export interface LdapConnectionTestResult {
  success: boolean;
  message: string;
}

export interface OrgChartNodeDto {
  id: string;
  username: string;
  displayName: string;
  managerId: string | null;
  photoBase64: string | null;
  unresolvedManagerName: string | null;
}

export interface OrgChartResultDto {
  hasManagerMapping: boolean;
  nodes: OrgChartNodeDto[];
}

export interface ConfidenceScoreSettingsDto {
  id: string;
  weightDescriptionLength: number;
  weightSpecificity: number;
  weightGenericPenalty: number;
  weightDuplicateDetection: number;
  weightRoundHoursSingle: number;
  weightDurationDescriptionRatio: number;
  weightDailyRoundTotal: number;
  weightDailyTotalReasonableness: number;
  weightBaselineDeviation: number;
  weightWeekendHoliday: number;
  thresholdVeryLow: number;
  thresholdLow: number;
  thresholdMedium: number;
  thresholdHigh: number;
  baselineLookbackDays: number;
  duplicateLookbackDays: number;
  duplicateSimilarityThreshold: number;
  shortDescriptionCharThreshold: number;
  longDescriptionCharThreshold: number;
  longDurationHoursThreshold: number;
  shortDurationHoursThreshold: number;
  dailyTotalSuspiciousHours: number;
  genericPhrasesCsv: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystemAdmin: boolean;
  permissionCount: number;
}

export interface RoleAssignedUserDto {
  id: string;
  username: string;
  displayName: string | null;
}

export interface PermissionDescriptorDto {
  key: string;
  moduleLabel: string;
  label: string;
}

export interface RoleDetailDto {
  id: string;
  name: string;
  description: string | null;
  isSystemAdmin: boolean;
  permissions: string[];
  assignedUsers: RoleAssignedUserDto[];
}
