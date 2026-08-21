// src/Core/DataAccess/PagedResult.cs ile birebir aynı sözleşme (System.Text.Json camelCase).
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
}

// src/Business/DTOs/Clubs/ClubListItemDto.cs
export interface ClubListItemDto {
  id: number
  name: string
  description: string | null
  isActive: boolean
  logoFileId: number | null
}

// src/Entities/Enums/ApplicationStatus.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type ApplicationStatus = 'Pending' | 'Approved' | 'Rejected'

// src/Business/DTOs/Memberships/MembershipApplicationListItemDto.cs
export interface MembershipApplicationListItemDto {
  id: number
  clubId: number
  clubName: string
  studentId: number
  studentNumber: string
  status: ApplicationStatus
  appliedAtUtc: string
}

// WebAPI/Extensions/ResultExtensions.cs · ToProblemResult
export interface ProblemDetailsResponse {
  status: number
  title: string
}

// src/Business/DTOs/Reports/ReportType.cs
export type ReportType = 'ClubMembers' | 'EventParticipants'

// src/Entities/Enums/ReportStatus.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type ReportStatus = 'Queued' | 'Processing' | 'Ready' | 'Failed'

// src/Business/DTOs/Reports/ReportRequestListItemDto.cs
export interface ReportRequestListItemDto {
  id: number
  reportType: ReportType
  status: ReportStatus
  requestedAtUtc: string
  completedAtUtc: string | null
  hasFile: boolean
  errorMessage: string | null
}

// src/Entities/Dtos/Reports/TermSummaryRowDto.cs
export interface TermSummaryRowDto {
  academicTermId: number
  termName: string
  clubCount: number
  memberCount: number
  eventCount: number
}

// src/Business/DTOs/Admin/PermissionCatalogItemDto.cs
export interface PermissionCatalogItemDto {
  code: string
  displayName: string
  description: string
}

// src/Business/DTOs/Admin/RoleListItemDto.cs
export interface RoleListItemDto {
  id: number
  name: string
  isSystemRole: boolean
  permissions: string[]
}

// src/Business/DTOs/Admin/UserListItemDto.cs
export interface UserListItemDto {
  id: number
  email: string
  roles: string[]
}

// src/Business/DTOs/Reference/FacultyListItemDto.cs
export interface FacultyListItemDto {
  id: number
  name: string
}

// src/Business/DTOs/Reference/DepartmentListItemDto.cs
export interface DepartmentListItemDto {
  id: number
  name: string
  facultyId: number
}

// src/Business/DTOs/Reference/AcademicTermListItemDto.cs
export interface AcademicTermListItemDto {
  id: number
  name: string
  startDateUtc: string
  endDateUtc: string
  isCurrent: boolean
}

// src/Entities/Enums/EventStatus.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type EventStatus = 'Draft' | 'PendingApproval' | 'Published' | 'Rejected'

// src/Business/DTOs/Events/EventListItemDto.cs
export interface EventListItemDto {
  id: number
  clubId: number
  clubName: string
  title: string
  description: string | null
  location: string | null
  startDateUtc: string
  endDateUtc: string
  capacity: number | null
  status: EventStatus
}
