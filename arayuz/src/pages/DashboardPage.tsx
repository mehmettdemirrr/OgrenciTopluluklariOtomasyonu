import { useQuery } from '@tanstack/react-query'
import { Box, Chip, List, ListItem, ListItemText, Stack, Typography } from '@mui/material'
import { BarChart } from '@mui/x-charts/BarChart'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { DateBadge } from '../components/ui/DateBadge'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { StatCard } from '../components/ui/StatCard'
import { brand } from '../theme/tokens'
import type { DashboardSummaryDto, EventListItemDto, MembershipApplicationListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function DashboardPage() {
  useDocumentTitle('Panel')

  const { hasPermission } = useAuth()
  const canReviewApplications = hasPermission(Permissions.MembershipsWrite)

  const summaryQuery = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: async () => (await apiClient.get<DashboardSummaryDto>('/dashboard')).data,
  })

  const upcomingQuery = useQuery({
    queryKey: ['events-upcoming', 0, 5],
    queryFn: async () => (await apiClient.get<PagedResult<EventListItemDto>>('/events/upcoming', { params: { pageIndex: 0, pageSize: 5 } })).data,
  })

  const pendingApplicationsQuery = useQuery({
    queryKey: ['membership-applications', 0, 5],
    enabled: canReviewApplications,
    queryFn: async () =>
      (await apiClient.get<PagedResult<MembershipApplicationListItemDto>>('/membership-applications', { params: { pageIndex: 0, pageSize: 5 } })).data,
  })

  const summary = summaryQuery.data
  const management = summary?.management
  const termTrend = summary?.termTrend ?? []

  return (
    <>
      <PageHeader title="Panel" description="Topluluk etkinliğinize dair kişisel özetiniz ve yönetim kapsamınız." />

      {summary && (
        <Stack spacing={3}>
          <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
            <StatCard label="Kulüplerim" value={summary.personal.myClubCount} icon={GroupsOutlinedIcon} />
            <StatCard label="Bekleyen Başvurularım" value={summary.personal.myPendingApplicationCount} icon={PendingActionsOutlinedIcon} />
            <StatCard label="Yaklaşan Etkinliklerim" value={summary.personal.myUpcomingEventCount} icon={EventOutlinedIcon} />
          </Stack>

          {management && (
            <SectionCard
              title="Yönetim Kapsamım"
              action={management.allClubs ? <Chip size="small" label="Tüm topluluklar" color="primary" variant="outlined" /> : undefined}
            >
              <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
                <StatCard label="Kapsamdaki Topluluk" value={management.scopeClubCount} icon={GroupsOutlinedIcon} color={brand.navy} />
                <StatCard label="Kapsamdaki Üye" value={management.scopeMemberCount} icon={GroupsOutlinedIcon} color={brand.navy} />
                <StatCard label="Bekleyen Başvuru" value={management.scopePendingApplicationCount} icon={FactCheckOutlinedIcon} color={brand.orange} />
                <StatCard label="Yaklaşan Etkinlik" value={management.scopeUpcomingEventCount} icon={EventOutlinedIcon} color={brand.turquoiseDark} />
              </Stack>
            </SectionCard>
          )}

          {termTrend.length > 1 && (
            <SectionCard title="Dönemlere göre kapsam trendi">
              <BarChart
                height={260}
                dataset={termTrend.map((row) => ({ ...row }))}
                xAxis={[{ dataKey: 'termName', scaleType: 'band' }]}
                colors={[brand.turquoise, brand.gold, brand.orange]}
                series={[
                  { dataKey: 'clubCount', label: 'Kulüp' },
                  { dataKey: 'memberCount', label: 'Üye' },
                  { dataKey: 'eventCount', label: 'Etkinlik' },
                ]}
              />
            </SectionCard>
          )}

          <SectionCard
            title="Yaklaşan Etkinlikler"
            action={
              <Typography component={RouterLink} to="/events" variant="body2" sx={{ color: 'primary.main', textDecoration: 'none', fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: 0.5 }}>
                Tümünü Gör <ArrowForwardRoundedIcon sx={{ fontSize: 16 }} />
              </Typography>
            }
          >
            {(upcomingQuery.data?.items.length ?? 0) === 0 ? (
              <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
            ) : (
              <List disablePadding>
                {upcomingQuery.data!.items.map((event) => (
                  <ListItem
                    key={event.id}
                    disableGutters
                    sx={{
                      py: 1.25,
                      px: 1,
                      mb: 0.75,
                      borderRadius: 2,
                      alignItems: 'flex-start',
                      gap: 1.5,
                      textDecoration: 'none',
                      color: 'inherit',
                      '&:hover': { bgcolor: 'action.hover' },
                    }}
                    component={RouterLink}
                    to={`/events/${event.id}`}
                  >
                    <DateBadge iso={event.startDateUtc} />
                    <ListItemText
                      primary={event.title}
                      secondary={`${event.clubName} · ${new Date(event.startDateUtc).toLocaleString('tr-TR')}`}
                      slotProps={{ primary: { sx: { fontWeight: 700 } } }}
                    />
                  </ListItem>
                ))}
              </List>
            )}
          </SectionCard>

          {canReviewApplications && (
            <SectionCard
              title="Bekleyen Üyelik Başvuruları"
              action={
                <Typography component={RouterLink} to="/review" variant="body2" sx={{ color: 'primary.main', textDecoration: 'none', fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: 0.5 }}>
                  Tümünü Gör <ArrowForwardRoundedIcon sx={{ fontSize: 16 }} />
                </Typography>
              }
            >
              {(pendingApplicationsQuery.data?.items.length ?? 0) === 0 ? (
                <EmptyState icon={FactCheckOutlinedIcon} title="Bekleyen başvuru yok" />
              ) : (
                <List disablePadding>
                  {pendingApplicationsQuery.data!.items.map((application) => (
                    <ListItem
                      key={application.id}
                      disableGutters
                      sx={{
                        py: 1.25,
                        px: 1,
                        mb: 0.75,
                        borderRadius: 2,
                        '&:hover': { bgcolor: 'action.hover' },
                      }}
                    >
                      <ListItemText
                        primary={`${application.studentNumber} · ${application.clubName}`}
                        secondary={new Date(application.appliedAtUtc).toLocaleString('tr-TR')}
                        slotProps={{ primary: { sx: { fontWeight: 700 } } }}
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </SectionCard>
          )}
        </Stack>
      )}

      {!summary && <Box sx={{ height: 4 }} />}
    </>
  )
}
