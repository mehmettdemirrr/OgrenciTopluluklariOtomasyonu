import { useQuery } from '@tanstack/react-query'
import { Box, Button, Card, CardContent, Chip, Grid, Stack, Typography, alpha, type SvgIconProps } from '@mui/material'
import GroupsRoundedIcon from '@mui/icons-material/GroupsRounded'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import { useEffect, type ComponentType } from 'react'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import { HomeAnnouncementsPanel } from '../../components/announcements/HomeAnnouncementsPanel'
import { HomeEventsCalendar } from '../../components/events/HomeEventsCalendar'
import { EmptyState } from '../../components/ui/EmptyState'
import { SectionCard } from '../../components/ui/SectionCard'
import type { PagedResult, PublicAnnouncementListItemDto, PublicClubListItemDto, PublicEventListItemDto, PublicStatsDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import universityLogo from '../../assets/logo.png'
import ozelPortrait from '../../assets/turgut-ozal-portrait.webp'
import { HomeLocationSection } from './HomeLocationSection'

export function HomePage() {
  const { t, dateLocale } = useLocale()
  const { hash } = useLocation()
  useDocumentTitle(t('home.title'))
  useEffect(() => {
    if (hash !== '#kampusler') {
      return
    }
    window.setTimeout(() => {
      document.getElementById('kampusler')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }, 50)
  }, [hash])

  const { isAuthenticated } = useAuth()

  const announcementsQuery = useQuery({
    queryKey: ['public-announcements', 0, 12],
    queryFn: async () =>
      (await apiClient.get<PagedResult<PublicAnnouncementListItemDto>>('/public/announcements', { params: { pageIndex: 0, pageSize: 12 } })).data,
  })

  const eventsQuery = useQuery({
    queryKey: ['public-events', 0, 6],
    queryFn: async () => (await apiClient.get<PagedResult<PublicEventListItemDto>>('/public/events', { params: { pageIndex: 0, pageSize: 6 } })).data,
  })

  const clubsQuery = useQuery({
    queryKey: ['public-clubs', 0, 6],
    queryFn: async () => (await apiClient.get<PagedResult<PublicClubListItemDto>>('/public/clubs', { params: { pageIndex: 0, pageSize: 6 } })).data,
  })

  const statsQuery = useQuery({
    queryKey: ['public-stats'],
    queryFn: async () => (await apiClient.get<PublicStatsDto>('/public/stats')).data,
  })

  return (
    <Box sx={{ position: 'relative' }}>
      <Box
        component="img"
        src={universityLogo}
        alt=""
        aria-hidden
        sx={{
          position: 'absolute',
          left: '50%',
          top: { xs: 220, md: 280 },
          transform: 'translateX(-50%)',
          width: { xs: 280, md: 560 },
          maxWidth: '88%',
          opacity: 0.1,
          pointerEvents: 'none',
          userSelect: 'none',
          zIndex: 0,
          filter: (theme) =>
            `drop-shadow(0 28px 48px ${alpha(theme.palette.secondary.main, 0.55)}) drop-shadow(0 8px 18px ${alpha(theme.palette.common.black, 0.28)})`,
        }}
      />
    <Stack spacing={5} sx={{ position: 'relative', zIndex: 1 }}>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1fr) 360px' },
          gap: 4,
          alignItems: 'stretch',
        }}
      >
      <Box
        sx={{
          position: 'relative',
          overflow: 'hidden',
          borderRadius: 4,
          minHeight: { xs: 380, md: 520 },
          height: { md: 520 },
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
            filter: (theme) =>
              `grayscale(0.2) contrast(1.12) brightness(1.08) drop-shadow(0 24px 40px ${alpha(theme.palette.common.black, 0.35)})`,
            WebkitMaskImage: (theme) =>
              `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.common.black, 0.2)} 22%, ${alpha(theme.palette.common.black, 0.85)} 52%, ${theme.palette.common.black} 100%)`,
            maskImage: (theme) =>
              `linear-gradient(90deg, transparent 0%, ${alpha(theme.palette.common.black, 0.2)} 22%, ${alpha(theme.palette.common.black, 0.85)} 52%, ${theme.palette.common.black} 100%)`,
          }}
        />
        <Box
          component="img"
          src={universityLogo}
          alt=""
          aria-hidden
          sx={{
            position: 'absolute',
            left: { xs: '52%', md: '36%' },
            top: '48%',
            transform: 'translate(-50%, -50%)',
            width: { xs: 240, md: 420 },
            opacity: { xs: 0.2, md: 0.28 },
            pointerEvents: 'none',
            userSelect: 'none',
            filter: (theme) =>
              `drop-shadow(0 22px 36px ${alpha(theme.palette.common.black, 0.55)}) drop-shadow(0 0 28px ${alpha(theme.palette.secondary.main, 0.45)})`,
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
          label={t('brand.university')}
          sx={{
            alignSelf: 'flex-start',
            bgcolor: (theme) => alpha(theme.palette.common.white, 0.12),
            color: 'common.white',
            fontWeight: 700,
            position: 'relative',
          }}
        />
        <Typography variant="h3" sx={{ fontWeight: 800, maxWidth: 680, fontSize: { xs: 32, md: 46 }, position: 'relative' }}>
          {t('home.headline')}
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.88, maxWidth: 560, lineHeight: 1.7, position: 'relative' }}>
          {t('home.lead')}
        </Typography>
        {!isAuthenticated && (
          <Stack direction="row" spacing={2} sx={{ mt: 1, position: 'relative', flexWrap: 'wrap' }}>
            <Button component={RouterLink} to="/register" variant="contained" size="large">
              {t('common.register')}
            </Button>
            <Button
              component={RouterLink}
              to="/login"
              variant="outlined"
              size="large"
              sx={{ color: 'common.white', borderColor: (theme) => alpha(theme.palette.common.white, 0.55) }}
            >
              {t('common.login')}
            </Button>
          </Stack>
        )}
        <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', position: 'relative' }}>
          <Chip icon={<GroupsRoundedIcon />} label={t('home.chipClubs')} variant="outlined" component={RouterLink} to="/kulupler" clickable sx={{ color: 'common.white', borderColor: (theme) => alpha(theme.palette.common.white, 0.35) }} />
          <Chip icon={<EventOutlinedIcon />} label={t('home.chipEvents')} variant="outlined" component={RouterLink} to="/etkinlikler" clickable sx={{ color: 'common.white', borderColor: (theme) => alpha(theme.palette.common.white, 0.35) }} />
          <Chip icon={<CampaignOutlinedIcon />} label={t('home.chipNews')} variant="outlined" component={RouterLink} to="/duyurular" clickable sx={{ color: 'common.white', borderColor: (theme) => alpha(theme.palette.common.white, 0.35) }} />
        </Stack>
      </Box>

      <HomeAnnouncementsPanel
        items={announcementsQuery.data?.items ?? []}
        totalCount={announcementsQuery.data?.totalCount ?? 0}
        loading={announcementsQuery.isLoading}
      />
      </Box>

      <SectionCard title={t('home.statsTitle')}>
        <HomeStatsBoxes
          items={[
            { key: 'clubs', label: t('home.statClubs'), value: statsQuery.data?.clubCount ?? clubsQuery.data?.totalCount ?? 0, icon: GroupsRoundedIcon, to: '/kulupler' },
            { key: 'active', label: t('home.statActive'), value: statsQuery.data?.activeClubCount ?? clubsQuery.data?.totalCount ?? 0, icon: VerifiedOutlinedIcon, to: '/kulupler' },
            { key: 'students', label: t('home.statStudents'), value: statsQuery.data?.studentCount ?? 0, icon: SchoolOutlinedIcon },
            { key: 'events', label: t('home.statEvents'), value: statsQuery.data?.upcomingEventCount ?? eventsQuery.data?.totalCount ?? 0, icon: EventOutlinedIcon, to: '/etkinlikler' },
          ]}
          dateLocale={dateLocale}
        />
      </SectionCard>

      <SectionCard
        title={t('home.calendarTitle')}
        action={
          <Button component={RouterLink} to="/etkinlikler" size="small" endIcon={<ArrowForwardRoundedIcon />}>
            {t('common.seeAll')}
          </Button>
        }
      >
        <HomeEventsCalendar />
      </SectionCard>

      <SectionCard
        title={t('home.clubs')}
        action={
          <Button component={RouterLink} to="/kulupler" size="small" endIcon={<ArrowForwardRoundedIcon />}>
            {t('common.seeAll')}
          </Button>
        }
      >
        {(clubsQuery.data?.items.length ?? 0) === 0 ? (
          <EmptyState icon={GroupsRoundedIcon} title={t('home.noClubs')} />
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

      <HomeLocationSection />
    </Stack>
    </Box>
  )
}

function HomeStatsBoxes({
  items,
  dateLocale,
}: {
  items: { key: string; label: string; value: number; icon: ComponentType<SvgIconProps>; to?: string }[]
  dateLocale: string
}) {
  return (
    <Grid container spacing={2}>
      {items.map((item) => {
        const Icon = item.icon
        return (
          <Grid key={item.key} size={{ xs: 6, md: 3 }}>
            <Box
              {...(item.to ? { component: RouterLink, to: item.to } : {})}
              sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 1.25,
                height: '100%',
                p: 2,
                borderRadius: 2.5,
                border: '1px solid',
                borderColor: 'divider',
                bgcolor: 'background.paper',
                textDecoration: 'none',
                color: 'inherit',
                transition: 'border-color 160ms ease, box-shadow 160ms ease',
                '&:hover': {
                  borderColor: 'primary.main',
                  boxShadow: (theme) => `0 10px 24px ${alpha(theme.palette.secondary.main, 0.08)}`,
                },
              }}
            >
              <Box
                sx={{
                  width: 36,
                  height: 36,
                  borderRadius: 2,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12),
                  color: 'primary.dark',
                }}
              >
                <Icon fontSize="small" />
              </Box>
              <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em', fontVariantNumeric: 'tabular-nums' }}>
                {item.value.toLocaleString(dateLocale)}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 700 }}>
                {item.label}
              </Typography>
            </Box>
          </Grid>
        )
      })}
    </Grid>
  )
}
