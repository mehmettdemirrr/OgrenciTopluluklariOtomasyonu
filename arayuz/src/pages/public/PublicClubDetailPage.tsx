import { useQuery } from '@tanstack/react-query'
import { Box, Card, CardContent, CardMedia, Chip, Grid, Skeleton, Stack, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import { useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { SectionCard } from '../../components/ui/SectionCard'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import type { PagedResult, PublicAnnouncementListItemDto, PublicClubDetailDto, PublicEventListItemDto } from '../../api/types'

export function PublicClubDetailPage() {
  const { id } = useParams<{ id: string }>()
  const clubId = Number(id)

  const clubQuery = useQuery({
    queryKey: ['public-club', clubId],
    queryFn: async () => (await apiClient.get<PublicClubDetailDto>(`/public/clubs/${clubId}`)).data,
  })

  useDocumentTitle(clubQuery.data?.name)

  const eventsQuery = useQuery({
    queryKey: ['public-events', 'club', clubId],
    queryFn: async () =>
      (await apiClient.get<PagedResult<PublicEventListItemDto>>('/public/events', { params: { clubId, pageIndex: 0, pageSize: 50 } })).data,
    enabled: clubQuery.isSuccess,
  })

  const announcementsQuery = useQuery({
    queryKey: ['public-announcements', 'club', clubId],
    queryFn: async () =>
      (await apiClient.get<PagedResult<PublicAnnouncementListItemDto>>('/public/announcements', { params: { clubId, pageIndex: 0, pageSize: 50 } })).data,
    enabled: clubQuery.isSuccess,
  })

  if (clubQuery.isLoading) {
    return <Skeleton variant="rounded" height={200} />
  }

  if (clubQuery.isError || !clubQuery.data) {
    return <EmptyState icon={GroupsOutlinedIcon} title="Kulüp bulunamadı" description="Bu kulüp mevcut değil ya da artık aktif değil." />
  }

  const club = clubQuery.data

  return (
    <Stack spacing={3}>
      <Stack direction="row" spacing={3} sx={{ alignItems: 'flex-start' }}>
        {club.logoFileId ? (
          <Box
            component="img"
            src={`/api/files/${club.logoFileId}`}
            alt=""
            sx={{ width: 96, height: 96, objectFit: 'contain', bgcolor: 'grey.50', borderRadius: 2, p: 1 }}
          />
        ) : (
          <Box sx={{ width: 96, height: 96, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50', borderRadius: 2 }}>
            <GroupsOutlinedIcon sx={{ fontSize: 40, color: 'grey.400' }} />
          </Box>
        )}
        <PageHeader title={club.name} description={club.description ?? undefined} />
      </Stack>

      <SectionCard title="Etkinlikler">
        {(eventsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
        ) : (
          <Grid container spacing={2}>
            {eventsQuery.data!.items.map((event) => (
              <Grid key={event.id} size={{ xs: 12, sm: 6 }}>
                <Card variant="outlined">
                  {event.posterFileId && (
                    <CardMedia component="img" height={120} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                  )}
                  <CardContent>
                    <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                      {event.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {new Date(event.startDateUtc).toLocaleString('tr-TR')}
                      {event.location ? ` · ${event.location}` : ''}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </SectionCard>

      <SectionCard title="Duyurular">
        {(announcementsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState title="Herkese açık duyuru yok" />
        ) : (
          <Stack spacing={2}>
            {announcementsQuery.data!.items.map((announcement) => (
              <Box key={announcement.id} sx={{ pb: 2, borderBottom: '1px solid', borderColor: 'divider', '&:last-of-type': { borderBottom: 0, pb: 0 } }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {announcement.title}
                  </Typography>
                  <Chip size="small" label={new Date(announcement.publishedAtUtc).toLocaleDateString('tr-TR')} variant="outlined" />
                </Stack>
                <Typography variant="body2">{announcement.content}</Typography>
              </Box>
            ))}
          </Stack>
        )}
      </SectionCard>
    </Stack>
  )
}
