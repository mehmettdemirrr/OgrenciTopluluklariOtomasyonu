import { Chip, type ChipProps } from '@mui/material'
import type { AnnouncementVisibility, ApplicationStatus, EventAudience, EventStatus, ReportStatus } from '../../api/types'

type ClubRole = 'Member' | 'Officer' | 'President'

type ChipColor = ChipProps['color']

const applicationStatusMap: Record<ApplicationStatus, { label: string; color: ChipColor }> = {
  Pending: { label: 'Bekliyor', color: 'warning' },
  Approved: { label: 'Onaylandı', color: 'success' },
  Rejected: { label: 'Reddedildi', color: 'error' },
}

const eventStatusMap: Record<EventStatus, { label: string; color: ChipColor }> = {
  Draft: { label: 'Taslak', color: 'default' },
  PendingApproval: { label: 'Onay Bekliyor', color: 'warning' },
  Published: { label: 'Yayında', color: 'success' },
  Rejected: { label: 'Reddedildi', color: 'error' },
  Cancelled: { label: 'İptal Edildi', color: 'error' },
}

const reportStatusMap: Record<ReportStatus, { label: string; color: ChipColor }> = {
  Queued: { label: 'Kuyrukta', color: 'warning' },
  Processing: { label: 'Üretiliyor', color: 'info' },
  Ready: { label: 'Hazır', color: 'success' },
  Failed: { label: 'Hatalı', color: 'error' },
}

const clubRoleMap: Record<ClubRole, { label: string; color: ChipColor }> = {
  Member: { label: 'Üye', color: 'default' },
  Officer: { label: 'Yönetici', color: 'info' },
  President: { label: 'Başkan', color: 'warning' },
}

const announcementVisibilityMap: Record<AnnouncementVisibility, { label: string; color: ChipColor }> = {
  Members: { label: 'Yalnızca Üyeler', color: 'default' },
  Public: { label: 'Herkese Açık', color: 'info' },
}

// K-38/A-65: duyuru görünürlüğünün etkinlik karşılığı — aynı biçim, aynı kural.
const eventAudienceMap: Record<EventAudience, { label: string; color: ChipColor }> = {
  Public: { label: 'Herkese Açık', color: 'info' },
  ClubMembers: { label: 'Üyelere Özel', color: 'default' },
}

function buildChip(entry: { label: string; color: ChipColor }, size: ChipProps['size']) {
  return <Chip label={entry.label} color={entry.color} size={size} variant={entry.color === 'default' ? 'outlined' : 'filled'} />
}

export function ApplicationStatusChip({ status, size = 'small' }: { status: ApplicationStatus; size?: ChipProps['size'] }) {
  return buildChip(applicationStatusMap[status], size)
}

export function EventStatusChip({ status, size = 'small' }: { status: EventStatus; size?: ChipProps['size'] }) {
  return buildChip(eventStatusMap[status], size)
}

export function ReportStatusChip({ status, size = 'small' }: { status: ReportStatus; size?: ChipProps['size'] }) {
  return buildChip(reportStatusMap[status], size)
}

export function ClubRoleChip({ role, size = 'small' }: { role: ClubRole; size?: ChipProps['size'] }) {
  return buildChip(clubRoleMap[role], size)
}

export function AnnouncementVisibilityChip({ visibility, size = 'small' }: { visibility: AnnouncementVisibility; size?: ChipProps['size'] }) {
  return buildChip(announcementVisibilityMap[visibility], size)
}

export function EventAudienceChip({ audience, size = 'small' }: { audience: EventAudience; size?: ChipProps['size'] }) {
  return buildChip(eventAudienceMap[audience], size)
}
