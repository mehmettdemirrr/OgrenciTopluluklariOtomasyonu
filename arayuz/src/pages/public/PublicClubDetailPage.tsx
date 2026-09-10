import { useQuery } from '@tanstack/react-query'
import { Box, Button, Card, CardContent, CardMedia, Chip, Grid, Skeleton, Stack, Typography } from '@mui/material'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import PhoneOutlinedIcon from '@mui/icons-material/PhoneOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { useEffect } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { PublicAnnouncementGridCard } from '../../components/announcements/PublicAnnouncementGridCard'
import { BackButton } from '../../components/ui/BackButton'
import { ClubShareCard } from '../../components/clubs/ClubShareCard'
import { DateBadge } from '../../components/ui/DateBadge'
import { EmptyState } from '../../components/ui/EmptyState'
import { SectionCard } from '../../components/ui/SectionCard'
import { SocialLinkIcons } from '../../components/ui/SocialLinks'
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

  // docs/MIMARI.md · A-82: oturum başına bir kez; hata yutulur, sayfa etkilenmez.
  useEffect(() => {
    if (!clubQuery.isSuccess) {
      return
    }
    const key = `club-view-${clubId}`
    if (sessionStorage.getItem(key)) {
      return
    }
    sessionStorage.setItem(key, '1')
    apiClient.post(`/public/clubs/${clubId}/view`).catch(() => undefined)
  }, [clubQuery.isSuccess, clubId])

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

  return (
    <Stack spacing={3}>
      <BackButton to="/kulupler" />

      {/* Künye: daire logo + ad + rozetler */}
      <Card variant="outlined" sx={{ borderRadius: 3, textAlign: 'center', pt: 4, pb: 3, px: 2 }}>
        <Box
          component={club.logoFileId ? 'img' : 'div'}
          src={club.logoFileId ? `/api/files/${club.logoFileId}` : undefined}
          alt=""
          sx={{
            width: 108, height: 108, borderRadius: '50%', objectFit: 'contain',
            bgcolor: 'background.paper', border: '4px solid', borderColor: 'background.paper',
            boxShadow: 3, mx: 'auto', display: 'block', p: 1,
          }}
        />
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mt: 2 }}>
          {club.name}
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2 }}>
          {/* Y-87: yıl yoksa rozet HİÇ çizilmez — CreatedAtUtc'den türetme. */}
          {club.foundedYear !== null && (
            <Chip icon={<CalendarMonthOutlinedIcon />} label={`${t('club.founded')}: ${club.foundedYear}`} />
          )}
          <Chip icon={<GroupsOutlinedIcon />} label={t('public.memberCount', { count: club.memberCount })} />
          <Chip icon={<VisibilityOutlinedIcon />} label={`${club.viewCount.toLocaleString(dateLocale)} ${t('club.views')}`} />
          <Chip icon={<EventOutlinedIcon />} label={t('public.eventCount', { count: club.eventCount })} />
          {club.clubCategoryNames.map((name) => (
            <Chip key={name} variant="outlined" color="primary" label={name} />
          ))}
        </Stack>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Stack spacing={2}>
            <Card variant="outlined" sx={{ borderRadius: 3 }}>
              <CardContent>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                  {t('club.status')}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ my: 1.5 }}>
                  {t('club.loginToJoinLead')}
                </Typography>
                <Button fullWidth size="large" variant="contained" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
                  {t('common.login')}
                </Button>
              </CardContent>
            </Card>

            {(club.contactEmail || club.contactPhone || club.socialLinks.length > 0) && (
              <Card variant="outlined" sx={{ borderRadius: 3 }}>
                <CardContent>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                    {t('club.contactSocial')}
                  </Typography>
                  <Stack spacing={1} sx={{ mt: 1.5 }}>
                    {club.contactEmail && (
                      <Stack direction="row" spacing={1} component="a" href={`mailto:${club.contactEmail}`} sx={{ alignItems: 'center', color: 'primary.main', textDecoration: 'none' }}>
                        <EmailOutlinedIcon fontSize="small" />
                        <Typography variant="body2">{club.contactEmail}</Typography>
                      </Stack>
                    )}
                    {club.contactPhone && (
                      <Stack direction="row" spacing={1} component="a" href={`tel:${club.contactPhone}`} sx={{ alignItems: 'center', color: 'text.primary', textDecoration: 'none' }}>
                        <PhoneOutlinedIcon fontSize="small" />
                        <Typography variant="body2">{club.contactPhone}</Typography>
                      </Stack>
                    )}
                    <SocialLinkIcons links={club.socialLinks} />
                  </Stack>
                </CardContent>
              </Card>
            )}

            <ClubShareCard clubId={club.id} clubName={club.name} />
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, md: 8 }}>
          <Stack spacing={3}>
            <Card variant="outlined" sx={{ borderRadius: 3 }}>
              <CardContent sx={{ p: { xs: 2.5, md: 3.5 } }}>
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
                  # {t('club.about')}
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ lineHeight: 1.9, whiteSpace: 'pre-line' }}>
                  {club.description || t('common.noDescription')}
                </Typography>
              </CardContent>
            </Card>

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
                <Grid container spacing={2.5}>
                  {announcementsQuery.data!.items.map((announcement) => (
                    <Grid key={announcement.id} size={{ xs: 12, sm: 6 }}>
                      <PublicAnnouncementGridCard announcement={announcement} />
                    </Grid>
                  ))}
                </Grid>
              )}
            </SectionCard>
          </Stack>
        </Grid>
      </Grid>
    </Stack>
  )
}
