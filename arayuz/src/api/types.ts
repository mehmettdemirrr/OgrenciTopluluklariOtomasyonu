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
