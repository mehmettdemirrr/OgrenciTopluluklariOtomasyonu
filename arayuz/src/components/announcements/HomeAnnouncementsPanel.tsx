import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import { Box, Button, Skeleton, Stack, Typography, alpha, useMediaQuery } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import type { PublicAnnouncementListItemDto } from '../../api/types'
import { useLocale } from '../../i18n/LocaleContext'
import { formatAnnouncementPill } from '../../utils/announcementFormat'

interface HomeAnnouncementsPanelProps {
  items: PublicAnnouncementListItemDto[]
  totalCount: number
  loading?: boolean
}

export function HomeAnnouncementsPanel({ items, totalCount, loading = false }: HomeAnnouncementsPanelProps) {
  const { t, dateLocale } = useLocale()
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)')
  const shouldScroll = !reduceMotion && items.length >= 4
  const rows = shouldScroll ? [...items, ...items] : items
  const durationSec = Math.max(18, items.length * 3.6)

  return (
    <Box
      sx={{
        height: { xs: 420, md: 520 },
        borderRadius: 4,
        p: { xs: 2.5, md: 4 },
        display: 'flex',
        flexDirection: 'column',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: (theme) => alpha(theme.palette.secondary.main, 0.08),
        boxShadow: (theme) => `0 16px 40px ${alpha(theme.palette.secondary.main, 0.06)}`,
        overflow: 'hidden',
      }}
    >
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 2.5 }}>
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', minWidth: 0 }}>
          <CampaignOutlinedIcon sx={{ color: 'primary.main' }} />
          <Typography variant="h6" component="h2" sx={{ fontWeight: 800, color: 'text.primary' }} noWrap>
            {t('home.latestAnnouncements')}
          </Typography>
        </Stack>
        <Box
          component="span"
          sx={{
            flexShrink: 0,
            px: 1.5,
            py: 0.5,
            borderRadius: 999,
            bgcolor: (theme) => alpha(theme.palette.warning.main, 0.15),
            color: 'warning.main',
            fontSize: 11,
            fontWeight: 800,
            letterSpacing: '0.08em',
            textTransform: 'uppercase',
            animation: reduceMotion ? 'none' : 'homeAnnPulse 2s ease-out infinite',
            '@keyframes homeAnnPulse': {
              '0%': { boxShadow: (theme) => `0 0 0 0 ${alpha(theme.palette.warning.main, 0.4)}` },
              '70%': { boxShadow: (theme) => `0 0 0 6px ${alpha(theme.palette.warning.main, 0)}` },
              '100%': { boxShadow: (theme) => `0 0 0 0 ${alpha(theme.palette.warning.main, 0)}` },
            },
          }}
        >
          {t('home.activeCount', { count: totalCount })}
        </Box>
      </Stack>

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', position: 'relative' }}>
        {loading ? (
          <Stack spacing={1.5}>
            {Array.from({ length: 4 }, (_, index) => (
              <Skeleton key={index} variant="rounded" height={72} />
            ))}
          </Stack>
        ) : items.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('home.noAnnouncements')}
          </Typography>
        ) : (
          <Box
            sx={{
              animation: shouldScroll ? `homeAnnScroll ${durationSec}s linear infinite` : 'none',
              '@keyframes homeAnnScroll': {
                from: { transform: 'translateY(0)' },
                to: { transform: 'translateY(-50%)' },
              },
              '&:hover': { animationPlayState: 'paused' },
            }}
          >
            {rows.map((announcement, index) => {
              const duplicate = index >= items.length
              return (
              <Box
                key={`${announcement.id}-${index}`}
                component={RouterLink}
                to={`/duyurular/${announcement.id}`}
                aria-hidden={duplicate || undefined}
                tabIndex={duplicate ? -1 : undefined}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                  textDecoration: 'none',
                  color: 'inherit',
                  px: 2,
                  py: 1.75,
                  mb: 1.5,
                  borderRadius: 2.5,
                  bgcolor: 'background.default',
                  border: '1px solid transparent',
                  transition: 'transform 180ms ease, border-color 180ms ease, box-shadow 180ms ease, background-color 180ms ease',
                  '&:hover': {
                    transform: reduceMotion ? 'none' : 'translateX(6px) translateY(-2px)',
                    bgcolor: 'background.paper',
                    borderColor: (theme) => alpha(theme.palette.primary.main, 0.2),
                    boxShadow: (theme) => `0 8px 20px ${alpha(theme.palette.secondary.main, 0.06)}`,
                  },
                }}
              >
                <Box
                  component="span"
                  sx={{
                    flexShrink: 0,
                    px: 1.25,
                    py: 0.4,
                    borderRadius: 1,
                    bgcolor: 'secondary.main',
                    color: 'common.white',
                    fontSize: 11,
                    fontWeight: 800,
                    letterSpacing: '0.06em',
                    textTransform: 'uppercase',
                  }}
                >
                  {formatAnnouncementPill(announcement.publishedAtUtc, dateLocale)}
                </Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    fontWeight: 700,
                    color: 'text.primary',
                    lineHeight: 1.45,
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    overflow: 'hidden',
                  }}
                >
                  {announcement.title}
                </Typography>
              </Box>
              )
            })}
          </Box>
        )}
      </Box>

      <Button
        component={RouterLink}
        to="/duyurular"
        endIcon={<ArrowForwardRoundedIcon />}
        sx={{
          mt: 1.5,
          py: 1.5,
          borderRadius: 1.5,
          fontWeight: 700,
          color: 'primary.main',
          bgcolor: (theme) => alpha(theme.palette.primary.main, 0.05),
          '&:hover': {
            color: 'common.white',
            bgcolor: 'primary.main',
          },
        }}
      >
        {t('home.allAnnouncements')}
      </Button>
    </Box>
  )
}
