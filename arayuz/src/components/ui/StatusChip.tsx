import { Chip, type ChipProps } from '@mui/material'
// K-36: ClubRole'ün tek tanımı api/types.ts'te — burada ikinci bir kopya tutulmuyor.
import type { AnnouncementVisibility, ApplicationStatus, ClubRole, EventAudience, EventStatus, ReportStatus } from '../../api/types'
import { useLocale } from '../../i18n/LocaleContext'

type ChipColor = ChipProps['color']

const applicationStatusMap: Record<ApplicationStatus, { labelKey: string; color: ChipColor }> = {
  Pending: { labelKey: 'status.pending', color: 'warning' },
  Approved: { labelKey: 'status.approved', color: 'success' },
  Rejected: { labelKey: 'status.rejected', color: 'error' },
}

const eventStatusMap: Record<EventStatus, { labelKey: string; color: ChipColor }> = {
  Draft: { labelKey: 'status.draft', color: 'default' },
  PendingApproval: { labelKey: 'status.pendingApproval', color: 'warning' },
  Published: { labelKey: 'status.published', color: 'success' },
  Rejected: { labelKey: 'status.rejected', color: 'error' },
  Cancelled: { labelKey: 'status.cancelled', color: 'error' },
}

const reportStatusMap: Record<ReportStatus, { labelKey: string; color: ChipColor }> = {
  Queued: { labelKey: 'status.queued', color: 'warning' },
  Processing: { labelKey: 'status.processing', color: 'info' },
  Ready: { labelKey: 'status.ready', color: 'success' },
  Failed: { labelKey: 'status.failed', color: 'error' },
}

const clubRoleMap: Record<ClubRole, { labelKey: string; color: ChipColor }> = {
  Member: { labelKey: 'status.member', color: 'default' },
  Officer: { labelKey: 'status.officer', color: 'info' },
  President: { labelKey: 'status.president', color: 'warning' },
}

const announcementVisibilityMap: Record<AnnouncementVisibility, { labelKey: string; color: ChipColor }> = {
  Members: { labelKey: 'status.membersOnly', color: 'default' },
  Public: { labelKey: 'status.public', color: 'info' },
}

// K-38/A-65: duyuru görünürlüğünün etkinlik karşılığı — aynı biçim, aynı kural.
const eventAudienceMap: Record<EventAudience, { labelKey: string; color: ChipColor }> = {
  Public: { labelKey: 'status.public', color: 'info' },
  ClubMembers: { labelKey: 'status.clubMembers', color: 'default' },
}

function buildChip(entry: { label: string; color: ChipColor }, size: ChipProps['size']) {
  return <Chip label={entry.label} color={entry.color} size={size} variant={entry.color === 'default' ? 'outlined' : 'filled'} />
}

function useMappedChip(entry: { labelKey: string; color: ChipColor }, size: ChipProps['size']) {
  const { t } = useLocale()
  return buildChip({ label: t(entry.labelKey), color: entry.color }, size)
}

export function ApplicationStatusChip({ status, size = 'small' }: { status: ApplicationStatus; size?: ChipProps['size'] }) {
  return useMappedChip(applicationStatusMap[status], size)
}

export function EventStatusChip({ status, size = 'small' }: { status: EventStatus; size?: ChipProps['size'] }) {
  return useMappedChip(eventStatusMap[status], size)
}

export function ReportStatusChip({ status, size = 'small' }: { status: ReportStatus; size?: ChipProps['size'] }) {
  return useMappedChip(reportStatusMap[status], size)
}

export function ClubRoleChip({ role, size = 'small' }: { role: ClubRole; size?: ChipProps['size'] }) {
  return useMappedChip(clubRoleMap[role], size)
}

export function AnnouncementVisibilityChip({ visibility, size = 'small' }: { visibility: AnnouncementVisibility; size?: ChipProps['size'] }) {
  return useMappedChip(announcementVisibilityMap[visibility], size)
}

export function EventAudienceChip({ audience, size = 'small' }: { audience: EventAudience; size?: ChipProps['size'] }) {
  return useMappedChip(eventAudienceMap[audience], size)
}
