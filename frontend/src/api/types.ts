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
