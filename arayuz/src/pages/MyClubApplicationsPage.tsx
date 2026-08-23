import { useQuery } from '@tanstack/react-query'
import { Stack, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { apiClient } from '../api/client'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { ApplicationStatusChip } from '../components/ui/StatusChip'
import type { ClubApplicationListItemDto } from '../api/types'

export function MyClubApplicationsPage() {
  const myApplicationsQuery = useQuery({
    queryKey: ['club-applications-mine'],
    queryFn: async () => (await apiClient.get<ClubApplicationListItemDto[]>('/club-applications/mine')).data,
  })

  const items = myApplicationsQuery.data ?? []

  return (
    <>
      <PageHeader title="Topluluk Kurma Başvurularım" description="Gönderdiğiniz topluluk kurma başvurularının durumu." />

      {!myApplicationsQuery.isLoading && items.length === 0 ? (
        <EmptyState
          icon={GroupsOutlinedIcon}
          title="Henüz bir başvurunuz yok"
          description="Kulüpler sayfasından yeni bir topluluk kurma başvurusu yapabilirsiniz."
        />
      ) : (
        <Stack spacing={2}>
          {items.map((application) => (
            <SectionCard key={application.id}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {application.proposedName}
                </Typography>
                <ApplicationStatusChip status={application.status} />
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                Önerilen danışman: {application.proposedAdvisorTitle}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {new Date(application.appliedAtUtc).toLocaleString('tr-TR')} tarihinde başvuruldu
              </Typography>
              {application.reviewNote && (
                <Typography variant="body2" sx={{ mt: 1 }}>
                  Not: {application.reviewNote}
                </Typography>
              )}
            </SectionCard>
          ))}
        </Stack>
      )}
    </>
  )
}
