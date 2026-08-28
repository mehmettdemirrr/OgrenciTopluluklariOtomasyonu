import { useQuery } from '@tanstack/react-query'
import { Box, Button, Card, CardContent, CardMedia, Chip, Grid, Stack, Typography, alpha } from '@mui/material'
import GroupsRoundedIcon from '@mui/icons-material/GroupsRounded'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import { DateBadge } from '../../components/ui/DateBadge'
import { EmptyState } from '../../components/ui/EmptyState'
import { AnnouncementCard } from '../../components/ui/AnnouncementCard'
import { SectionCard } from '../../components/ui/SectionCard'
import type { PagedResult, PublicAnnouncementListItemDto, PublicClubListItemDto, PublicEventListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import ozelPortrait from '../../assets/turgut-ozal-portrait.webp'

export function HomePage() {
  useDocumentTitle('Ana Sayfa')

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
          position: 'relative',
          overflow: 'hidden',
          borderRadius: 4,
          minHeight: { xs: 380, md: 480 },
          p: { xs: 4, md: 7 },
          color: 'common.white',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          gap: 2,
          background: (theme) =>
            `linear-gradient(135deg, ${theme.palette.secondary.main} 0%, ${theme.palette.primary.dark} 58%, ${theme.palette.secondary.main} 100%)`,
        }}
      >
        <Box
          component="img"
          src={ozelPortrait}
          alt=""
          aria-hidden
          sx={{
            position: 'absolute',
            right: { xs: -32, md: -8 },
            top: { xs: 24, md: '50%' },
            transform: { md: 'translateY(-50%)' },
            height: { xs: '92%', md: '130%' },
            width: { xs: '78%', sm: '58%', md: '48%' },
            objectFit: 'cover',
            objectPosition: 'center 18%',
            pointerEvents: 'none',
            userSelect: 'none',
            opacity: { xs: 0.22, md: 0.42 },
            mixBlendMode: 'luminosity',
            filter: 'grayscale(0.2) contrast(1.12) brightness(1.08)',
            WebkitMaskImage: (theme) =>
              `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.common.black, 0.2)} 22%, ${alpha(theme.palette.common.black, 0.85)} 52%, ${theme.palette.common.black} 100%)`,
            maskImage: (theme) =>
              `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.common.black, 0.2)} 22%, ${alpha(theme.palette.common.black, 0.85)} 52%, ${theme.palette.common.black} 100%)`,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            pointerEvents: 'none',
            background: (theme) =>
              `linear-gradient(105deg, ${theme.palette.secondary.main} 0%, ${alpha(theme.palette.secondary.main, 0.72)} 38%, ${alpha(theme.palette.primary.dark, 0.18)} 68%, transparent 100%)`,
          }}
        />
        <Chip
          label="Malatya Turgut Özal Üniversitesi"
          sx={{
            alignSelf: 'flex-start',
            bgcolor: (theme) => alpha(theme.palette.common.white, 0.12),
            color: 'common.white',
            fontWeight: 700,
            position: 'relative',
          }}
        />
        <Typography variant="h3" sx={{ fontWeight: 800, maxWidth: 680, fontSize: { xs: 32, md: 46 }, position: 'relative' }}>
          Kampüsteki topluluklar, tek yerde.
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.88, maxWidth: 560, lineHeight: 1.7, position: 'relative' }}>
          Kulüpleri keşfedin, yaklaşan etkinlikleri görün, duyuruları takip edin — üye olmak için kayıt olmanız yeterli.
        </Typography>
        {!isAuthenticated && (
          <Stack direction="row" spacing={2} sx={{ mt: 1, position: 'relative', flexWrap: 'wrap' }}>
            <Button component={RouterLink} to="/register" variant="contained" size="large">
              Kayıt Ol
            </Button>
            <Button
              component={RouterLink}
              to="/login"
              variant="outlined"
              size="large"
              sx={{ color: 'common.white', borderColor: (theme) => alpha(theme.palette.common.white, 0.55) }}
            >
              Giriş Yap
            </Button>
          </Stack>
        )}
        <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', position: 'relative' }}>
          <Chip icon={<GroupsRoundedIcon />} label="Kulüp keşfi" variant="outlined" sx={{ color: 'common.white', borderColor: (t) => alpha(t.palette.common.white, 0.35) }} />
          <Chip icon={<EventOutlinedIcon />} label="Etkinlik takvimi" variant="outlined" sx={{ color: 'common.white', borderColor: (t) => alpha(t.palette.common.white, 0.35) }} />
          <Chip icon={<CampaignOutlinedIcon />} label="Duyurular" variant="outlined" sx={{ color: 'common.white', borderColor: (t) => alpha(t.palette.common.white, 0.35) }} />
        </Stack>
      </Box>

      <SectionCard title="Duyurular">
        {(announcementsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState title="Henüz herkese açık bir duyuru yok" />
        ) : (
          <Stack spacing={1.5}>
            {announcementsQuery.data!.items.map((announcement) => (
              <AnnouncementCard
                key={announcement.id}
                title={announcement.title}
                content={announcement.content}
                publishedAtUtc={announcement.publishedAtUtc}
                chip={announcement.clubName ? <Chip size="small" label={announcement.clubName} variant="outlined" /> : undefined}
              />
            ))}
          </Stack>
        )}
      </SectionCard>

      <SectionCard
        title="Yaklaşan Etkinlikler"
        action={
          <Button component={RouterLink} to="/etkinlikler" size="small" endIcon={<ArrowForwardRoundedIcon />}>
            Tümünü Gör
          </Button>
        }
      >
        {(eventsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
        ) : (
          <Grid container spacing={2.5}>
            {eventsQuery.data!.items.map((event) => (
              <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
                <Card variant="outlined" sx={{ height: '100%' }}>
                  {event.posterFileId && (
                    <CardMedia component="img" height={140} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                  )}
                  <CardContent>
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                      <DateBadge iso={event.startDateUtc} />
                      <Box sx={{ minWidth: 0 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 800 }} noWrap>
                          {event.title}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" noWrap>
                          {event.clubName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {new Date(event.startDateUtc).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
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

      <SectionCard
        title="Kulüpler"
        action={
          <Button component={RouterLink} to="/kulupler" size="small" endIcon={<ArrowForwardRoundedIcon />}>
            Tümünü Gör
          </Button>
        }
      >
        {(clubsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={GroupsRoundedIcon} title="Henüz aktif bir kulüp yok" />
        ) : (
          <Grid container spacing={2.5}>
            {clubsQuery.data!.items.map((club) => (
              <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
                <Card
                  variant="outlined"
                  component={RouterLink}
                  to={`/kulupler/${club.id}`}
                  sx={{ display: 'flex', flexDirection: 'column', height: '100%', textDecoration: 'none', color: 'inherit' }}
                >
                  <Box
                    sx={{
                      height: 88,
                      background: (theme) =>
                        `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.92)} 0%, ${alpha(theme.palette.primary.dark, 0.88)} 100%)`,
                      display: 'flex',
                      alignItems: 'flex-end',
                      px: 2,
                      pb: 1.5,
                    }}
                  >
                    <GroupsRoundedIcon sx={{ color: 'common.white', fontSize: 28, opacity: 0.9 }} />
                  </Box>
                  <CardContent sx={{ flex: 1 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 800, mb: 0.75 }} noWrap>
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
