// src/DataAccess/Seed/IdentitySeedData.cs · Permissions ile birebir aynı string sözleşmesi —
// yetki kararı burada değil, API'de verilir (bu yalnızca görüntü katmanı gizleme/gösterme içindir).
export const Permissions = {
  ClubsRead: 'clubs.read',
  ClubsWrite: 'clubs.write',
  MembershipsRead: 'memberships.read',
  MembershipsWrite: 'memberships.write',
  EventsRead: 'events.read',
  EventsWrite: 'events.write',
  DiagnosticsProtected: 'diagnostics.protected',
  HangfireDashboard: 'hangfire.dashboard',
  ReportsRead: 'reports.read',
  ReportsReadAll: 'reports.read.all',
  FilesUpload: 'files.upload',
  RolesManage: 'roles.manage',
  ReferenceManage: 'reference.manage',
  EventsApprove: 'events.approve',
  AnnouncementsWrite: 'announcements.write',
  AnnouncementsGlobal: 'announcements.global',
} as const
