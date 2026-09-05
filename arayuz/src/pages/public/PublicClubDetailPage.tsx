import { useQuery } from '@tanstack/react-query'
import { Box, Card, CardContent, CardMedia, Chip, Grid, Skeleton, Stack, Typography } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { AnnouncementCard } from '../../components/ui/AnnouncementCard'
import { BackButton } from '../../components/ui/BackButton'
import { DateBadge } from '../../components/ui/DateBadge'
import { DetailHero, DetailMedia } from '../../components/ui/DetailHero'
import { EmptyState } from '../../components/ui/EmptyState'
import { InfoTile } from '../../components/ui/InfoTile'
import { SectionCard } from '../../components/ui/SectionCard'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import type { PagedResult, PublicAnnouncementListItemDto, PublicClubDetailDto, PublicEventListItemDto } from '../../api/types'

export function PublicClubDetailPage() {
  const { t, dateLocale } = useLocale()
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
    return (
      <Stack spacing={2}>
        <BackButton to="/kulupler" />
        <Skeleton variant="rounded" height={260} />
      </Stack>
    )
  }

  if (clubQuery.isError || !clubQuery.data) {
    return (
      <Stack spacing={2}>
        <BackButton to="/kulupler" />
        <EmptyState icon={GroupsOutlinedIcon} title={t('public.clubMissing')} description={t('public.clubMissingLead')} />
      </Stack>
    )
  }

  const club = clubQuery.data
  const eventCount = eventsQuery.data?.totalCount ?? eventsQuery.data?.items.length ?? 0
  const announcementCount = announcementsQuery.data?.totalCount ?? announcementsQuery.data?.items.length ?? 0

  return (
    <Stack spacing={3}>
      <BackButton to="/kulupler" />
      <DetailHero
        media={
          <DetailMedia
            src={club.logoFileId ? `/api/files/${club.logoFileId}` : null}
            alt=""
            fallback={<GroupsOutlinedIcon sx={{ fontSize: 48, color: 'primary.dark' }} />}
          />
        }
        chips={club.clubCategoryName ? <Chip size="small" variant="outlined" color="primary" label={club.clubCategoryName} /> : undefined}
        title={club.name}
        titleComponent="h1"
        description={club.description}
      >
        <Grid container spacing={1.5}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <InfoTile icon={EventOutlinedIcon} label={t('public.eventLabel')} value={t('public.eventsCount', { count: eventCount })} />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <InfoTile icon={CampaignOutlinedIcon} label={t('public.announcementLabel')} value={t('public.announcementsCount', { count: announcementCount })} />
          </Grid>
        </Grid>
      </DetailHero>

      <SectionCard title={t('pages.events')}>
        {(eventsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={EventOutlinedIcon} title={t('home.noEvents')} />
        ) : (
          <Grid container spacing={2}>
            {eventsQuery.data!.items.map((event) => (
              <Grid key={event.id} size={{ xs: 12, sm: 6 }}>
                <Card variant="outlined">
                  {event.posterFileId && (
                    <CardMedia component="img" height={140} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                  )}
                  <CardContent>
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                      <DateBadge iso={event.startDateUtc} />
                      <Box sx={{ minWidth: 0 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
                          {event.title}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, lineHeight: 1.6 }}>
                          {new Date(event.startDateUtc).toLocaleString(dateLocale)}
                          {event.location ? ` · ${event.location}` : ''}
                        </Typography>
                      </Box>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </SectionCard>

      <SectionCard title={t('pages.announcements')}>
        {(announcementsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState title={t('public.noPublicAnnouncements')} />
        ) : (
          <Stack spacing={1.5}>
            {announcementsQuery.data!.items.map((announcement) => (
              <AnnouncementCard
                key={announcement.id}
                title={announcement.title}
                content={announcement.content}
                contentJson={announcement.contentJson}
                imageFileId={announcement.imageFileId}
                publishedAtUtc={announcement.publishedAtUtc}
              />
            ))}
          </Stack>
        )}
      </SectionCard>
    </Stack>
  )
}
