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
  clubCategoryId: number | null
  clubCategoryName: string | null
}

// src/Business/DTOs/Clubs/ClubDetailDto.cs
export interface ClubDetailDto {
  id: number
  name: string
  description: string | null
  isActive: boolean
  createdAtUtc: string
  advisorId: number
  logoFileId: number | null
  clubCategoryId: number | null
  clubCategoryName: string | null
}

// src/Business/DTOs/Reference/ClubCategoryListItemDto.cs
export interface ClubCategoryListItemDto {
  id: number
  name: string
}

// src/Entities/Enums/ClubRole.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type ClubRole = 'Member' | 'Officer' | 'President'

// src/Entities/Enums/ClubCapability.cs — [Flags] enum SAYI olarak taşınır (diğer enum'lar metin).
// Bunu Program.cs'teki JsonNumberEnumConverter<ClubCapability> sağlıyor ve o satır
// JsonStringEnumConverter'dan ÖNCE gelmek zorunda. Aksi hâlde "EventsManage, AnnouncementsManage"
// gibi virgüllü metin gelir, aşağıdaki bit maskesi NaN üretir ve her yetki boş görünür.
export const ClubCapability = {
  None: 0,
  MembersView: 1,
  MembersManage: 2,
  EventsManage: 4,
  EventParticipantsView: 8,
  AnnouncementsManage: 16,
  ReportsView: 32,
} as const

/** Kutucuk listesi — sıra ekranda göründüğü sıradır. */
export const CLUB_CAPABILITIES: { value: number; label: string; description: string }[] = [
  { value: ClubCapability.EventsManage, label: 'Etkinlik yönetimi', description: 'Etkinlik oluşturabilir, düzenleyebilir ve silebilir.' },
  { value: ClubCapability.EventParticipantsView, label: 'Katılımcı listesi', description: 'Etkinliklere kimlerin kaydolduğunu görebilir.' },
  { value: ClubCapability.AnnouncementsManage, label: 'Duyuru yönetimi', description: 'Topluluk duyurusu yazabilir.' },
  { value: ClubCapability.MembersView, label: 'Üye listesi', description: 'Topluluk üyelerini görebilir.' },
  { value: ClubCapability.MembersManage, label: 'Üye ve rol yönetimi', description: 'Üye çıkarabilir, rol atayabilir, unvan tanımlayabilir.' },
  { value: ClubCapability.ReportsView, label: 'Raporlar', description: 'Topluluğun raporlarını alabilir.' },
]

// src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs
export interface ClubRoleDefinitionDto {
  id: number
  name: string
  /** A-68: makam — A-39 ve dönem devri için; yetki vermez. */
  clubRole: ClubRole
  /** A-68: yetki matrisi, bit maskesi. */
  capabilities: number
  displayOrder: number
}

// src/Business/DTOs/Clubs/ClubMemberListItemDto.cs
export interface ClubMemberListItemDto {
  membershipId: number
  studentId: number
  studentNumber: string
  clubRole: ClubRole
  clubRoleDefinitionId: number | null
  /** Görünen unvan (K-36). Null ise arayüz yetki seviyesine düşer. */
  clubRoleName: string | null
  joinedAtUtc: string
}

// src/Business/DTOs/Reference/AcademicStaffListItemDto.cs
export interface AcademicStaffListItemDto {
  id: number
  title: string
  email: string
  firstName: string | null
  lastName: string | null
}

// src/Business/DTOs/Reference/SelectableAcademicStaffDto.cs
export interface SelectableAcademicStaffDto {
  id: number
  title: string
  /** A-56: e-posta bu uctan kaldirildi; ad soyad yoksa e-postaya duser. */
  fullName: string
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
  reviewedAtUtc: string | null
}

// src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs
export interface ClubApplicationListItemDto {
  id: number
  studentId: number
  studentNumber: string
  proposedName: string
  description: string | null
  justification: string
  proposedAdvisorId: number
  proposedAdvisorDisplayName: string
  status: ApplicationStatus
  appliedAtUtc: string
  reviewedAtUtc: string | null
  reviewNote: string | null
  createdClubId: number | null
  proposedCategoryId: number | null
  proposedCategoryName: string | null
  logoFileId: number | null
  documents: ClubApplicationDocumentDto[]
}

// src/Business/DTOs/Reference/ClubDocumentTypeListItemDto.cs
export interface ClubDocumentTypeListItemDto {
  id: number
  code: string
  name: string
  isRequired: boolean
  isActive: boolean
  displayOrder: number
}

// src/Business/DTOs/ClubApplications/ClubApplicationDocumentDto.cs
export interface ClubApplicationDocumentDto {
  documentId: number
  documentTypeId: number
  code: string
  name: string
  isRequired: boolean
  originalFileName: string
  fileSizeBytes: number
}

// WebAPI/Extensions/ResultExtensions.cs · ToProblemResult
export interface ProblemDetailsResponse {
  status: number
  title: string
}

// src/Business/DTOs/Reports/ReportType.cs
export type ReportType = 'ClubMembers' | 'EventParticipants' | 'TermSummary'

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
  /** A-56: bos olabilir — arayuz e-postaya duser. */
  firstName: string | null
  lastName: string | null
  /** A-57: pasif hesap giris yapamaz. */
  isLockedOut: boolean
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

// src/Entities/Enums/ClubApplicationWindowOverride.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type ClubApplicationWindowOverride = 'FollowSchedule' | 'ForceOpen' | 'ForceClosed'

// src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs
export interface ClubApplicationWindowDto {
  isOpen: boolean
  startUtc: string | null
  endUtc: string | null
  override: ClubApplicationWindowOverride
  termName: string
}

// src/Business/DTOs/Reference/AcademicTermListItemDto.cs
export interface AcademicTermListItemDto {
  id: number
  name: string
  startDateUtc: string
  endDateUtc: string
  isCurrent: boolean
  clubApplicationStartUtc: string | null
  clubApplicationEndUtc: string | null
  clubApplicationOverride: ClubApplicationWindowOverride
}

// src/Entities/Enums/EventStatus.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type EventStatus = 'Draft' | 'PendingApproval' | 'Published' | 'Rejected' | 'Cancelled'

// src/Entities/Enums/EventAudience.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type EventAudience = 'Public' | 'ClubMembers'

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
  audience: EventAudience
  cancellationReason: string | null
  /** A-36: afiş görünürlüğü Public — anonim /api/files/{id} ucundan servis edilir. */
  posterFileId: number | null
  /** null = bu listede hesaplanmadı; yalnızca /events/upcoming doldurur (PLAN-V4 §21.5). */
  isRegistered: boolean | null
}

// src/Business/DTOs/Events/EventParticipantListItemDto.cs
export interface EventParticipantListItemDto {
  studentId: number
  studentNumber: string
  registeredAtUtc: string
}

// src/Entities/Enums/AnnouncementVisibility.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type AnnouncementVisibility = 'Members' | 'Public'

// src/Business/DTOs/Announcements/AnnouncementListItemDto.cs
export interface AnnouncementListItemDto {
  id: number
  clubId: number | null
  clubName: string | null
  title: string
  content: string
  visibility: AnnouncementVisibility
  /** docs/MIMARI.md · A-71: null ise content düz metin olarak gösterilir. */
  contentJson: string | null
  /** docs/MIMARI.md · K-42: kapak görseli. */
  imageFileId: number | null
  publishedAtUtc: string
}

// src/Business/DTOs/Auth/RegistrationDepartmentDto.cs
export interface RegistrationDepartmentDto {
  id: number
  name: string
  facultyName: string
}

// src/Business/DTOs/Auth/MeResponseDto.cs
export interface MeResponseDto {
  email: string
  /** A-56: boşsa arayüz e-postaya düşer. */
  firstName: string | null
  lastName: string | null
  roles: string[]
  permissions: string[]
  // Öğrenci profili — danışman/admin hesaplarında hepsi null.
  studentNumber: string | null
  departmentId: number | null
  departmentName: string | null
  facultyName: string | null
  enrollmentYear: number | null
}

// src/Business/DTOs/Clubs/MyClubMembershipDto.cs
// src/Entities/Enums/ClubRelationship.cs — backend enum'unun tamamı; bu ekran yalnızca
// Member ve Advisor üretir, kalan değerler ClubDetailDto.myRelationship (Faz 41) içindir.
export type ClubRelationship = 'None' | 'Member' | 'Officer' | 'President' | 'Advisor' | 'Administrator'

export interface MyClubMembershipDto {
  clubId: number
  clubName: string
  clubIsActive: boolean
  clubRole: ClubRole
  /** Görünen unvan (K-36). Null ise arayüz yetki seviyesine düşer. */
  clubRoleName: string | null
  joinedAtUtc: string
  /** Liste güncel döneme filtreli (PLAN-V4 §22.3). */
  academicTermName: string
  /** docs/MIMARI.md · K-41/A-70: Member = üyelik satırı, Advisor = danışmanlık satırı. */
  relationship: ClubRelationship
}

// src/Entities/Dtos/Dashboard/PersonalDashboardStatsDto.cs
export interface PersonalDashboardStatsDto {
  myClubCount: number
  myPendingApplicationCount: number
  myUpcomingEventCount: number
}

// src/Entities/Dtos/Dashboard/ManagementDashboardStatsDto.cs
export interface ManagementDashboardStatsDto {
  allClubs: boolean
  scopeClubCount: number
  scopeMemberCount: number
  scopePendingApplicationCount: number
  scopeUpcomingEventCount: number
}

// src/Business/DTOs/Dashboard/DashboardSummaryDto.cs
export interface DashboardSummaryDto {
  personal: PersonalDashboardStatsDto
  management: ManagementDashboardStatsDto | null
  termTrend: TermSummaryRowDto[]
}

// src/Business/DTOs/Public/PublicStatsDto.cs
export interface PublicStatsDto {
  clubCount: number
  activeClubCount: number
  studentCount: number
  upcomingEventCount: number
}

// src/Business/DTOs/Public/PublicClubCategoryDto.cs
export interface PublicClubCategoryDto {
  id: number
  name: string
}

// src/Business/DTOs/Public/PublicClubListItemDto.cs
export interface PublicClubListItemDto {
  id: number
  name: string
  description: string | null
  logoFileId: number | null
  /** K-35: kurumsal sınıflandırma; kimlik taşınmaz (Y-58). */
  clubCategoryName: string | null
}

// src/Business/DTOs/Public/PublicClubDetailDto.cs
export interface PublicClubDetailDto {
  id: number
  name: string
  description: string | null
  logoFileId: number | null
  clubCategoryName: string | null
}

// src/Business/DTOs/Public/PublicEventListItemDto.cs
export interface PublicEventListItemDto {
  id: number
  clubId: number
  clubName: string
  title: string
  description: string | null
  location: string | null
  startDateUtc: string
  endDateUtc: string
  capacity: number | null
  posterFileId: number | null
}

// src/Business/DTOs/Public/PublicAnnouncementListItemDto.cs
export interface PublicAnnouncementListItemDto {
  id: number
  clubId: number | null
  clubName: string | null
  title: string
  content: string
  /** docs/MIMARI.md · A-71: null ise content düz metin olarak gösterilir. */
  contentJson: string | null
  /** docs/MIMARI.md · K-42: kapak görseli. */
  imageFileId: number | null
  publishedAtUtc: string
}

// src/Entities/Enums/AuditAction.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type AuditAction = 'Insert' | 'Update' | 'Delete'

// src/Entities/Dtos/Audit/AuditLogListItemDto.cs
export interface AuditLogListItemDto {
  id: number
  userId: number | null
  entityType: string
  entityId: string
  action: AuditAction
  timestampUtc: string
  oldValues: string | null
  newValues: string | null
  correlationId: string | null
}

// src/Entities/Dtos/Traffic/TrafficLogListItemDto.cs
export interface TrafficLogListItemDto {
  id: number
  correlationId: string
  userId: number | null
  ipAddress: string
  userAgent: string | null
  httpMethod: string
  path: string
  redactedQueryString: string | null
  statusCode: number
  durationMs: number
  timestampUtc: string
}
