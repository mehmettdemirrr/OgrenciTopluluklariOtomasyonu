import { useQuery } from '@tanstack/react-query'
import { Box, Button, Card, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material'
import GroupsRoundedIcon from '@mui/icons-material/GroupsRounded'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import { EmptyState } from '../../components/ui/EmptyState'
import { SectionCard } from '../../components/ui/SectionCard'
import type { PagedResult, PublicAnnouncementListItemDto, PublicClubListItemDto, PublicEventListItemDto } from '../../api/types'

export function HomePage() {
  const { isAuthenticated } = useAuth()

  const announcementsQuery = useQuery({
    queryKey: ['public-announcements', 0, 4],
    queryFn: async () =>
      (await apiClient.get<PagedResult<PublicAnnouncementListItemDto>>('/public/announcements', { params: { pageIndex: 0, pageSize: 4 } })).data,
  })

  const eventsQuery = useQuery({
    queryKey: ['public-events', 0, 6],
    queryFn: async () => (await apiClient.get<PagedResult<PublicEventListItemDto>>('/public/events', { params: { pageIndex: 0, pageSize: 6 } })).data,
  })

  const clubsQuery = useQuery({
    queryKey: ['public-clubs', 0, 6],
    queryFn: async () => (await apiClient.get<PagedResult<PublicClubListItemDto>>('/public/clubs', { params: { pageIndex: 0, pageSize: 6 } })).data,
  })

  return (
    <Stack spacing={5}>
      <Box
        sx={{
          borderRadius: 3,
          p: { xs: 4, md: 6 },
          bgcolor: 'secondary.main',
          color: 'common.white',
          display: 'flex',
          flexDirection: 'column',
          gap: 2,
        }}
      >
        <Typography variant="h3" sx={{ fontWeight: 800, maxWidth: 640 }}>
          Kampüsteki topluluklar, tek yerde.
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.85, maxWidth: 560 }}>
          Kulüpleri keşfedin, yaklaşan etkinlikleri görün, duyuruları takip edin — üye olmak için kayıt olmanız yeterli.
        </Typography>
        {!isAuthenticated && (
          <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
            <Button component={RouterLink} to="/register" variant="contained" size="large">
              Kayıt Ol
            </Button>
            <Button component={RouterLink} to="/login" variant="outlined" size="large" sx={{ color: 'common.white', borderColor: 'common.white' }}>
              Giriş Yap
            </Button>
          </Stack>
        )}
      </Box>

      <SectionCard
        title="Duyurular"
        action={
          <Typography component={RouterLink} to="/etkinlikler" variant="body2" sx={{ color: 'primary.main', textDecoration: 'none' }}>
            Tüm Etkinlikler
          </Typography>
        }
      >
        {(announcementsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState title="Henüz herkese açık bir duyuru yok" />
        ) : (
          <Stack spacing={2}>
            {announcementsQuery.data!.items.map((announcement) => (
              <Box key={announcement.id} sx={{ pb: 2, borderBottom: '1px solid', borderColor: 'divider', '&:last-of-type': { borderBottom: 0, pb: 0 } }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                    {announcement.title}
                  </Typography>
                  {announcement.clubName && <Chip size="small" label={announcement.clubName} variant="outlined" />}
                </Stack>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  {new Date(announcement.publishedAtUtc).toLocaleDateString('tr-TR')}
                </Typography>
                <Typography variant="body2">{announcement.content}</Typography>
              </Box>
            ))}
          </Stack>
        )}
      </SectionCard>

      <SectionCard
        title="Yaklaşan Etkinlikler"
        action={
          <Typography component={RouterLink} to="/etkinlikler" variant="body2" sx={{ color: 'primary.main', textDecoration: 'none' }}>
            Tümünü Gör
          </Typography>
        }
      >
        {(eventsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
        ) : (
          <Grid container spacing={2}>
            {eventsQuery.data!.items.map((event) => (
              <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
                <Card variant="outlined">
                  {event.posterFileId && (
                    <CardMedia component="img" height={100} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                  )}
                  <CardContent>
                    <Typography variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>
                      {event.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {event.clubName}
                    </Typography>
                    <Typography variant="caption">{new Date(event.startDateUtc).toLocaleString('tr-TR')}</Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </SectionCard>

      <SectionCard
        title="Kulüpler"
        action={
          <Typography component={RouterLink} to="/kulupler" variant="body2" sx={{ color: 'primary.main', textDecoration: 'none' }}>
            Tümünü Gör
          </Typography>
        }
      >
        {(clubsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={GroupsRoundedIcon} title="Henüz aktif bir kulüp yok" />
        ) : (
          <Grid container spacing={2}>
            {clubsQuery.data!.items.map((club) => (
              <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
                <Card
                  variant="outlined"
                  component={RouterLink}
                  to={`/kulupler/${club.id}`}
                  sx={{ display: 'block', height: '100%', textDecoration: 'none', color: 'inherit' }}
                >
                  <CardContent>
                    <Typography variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>
                      {club.name}
                    </Typography>
                    {club.description && (
                      <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                        {club.description}
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </SectionCard>
    </Stack>
  )
}
