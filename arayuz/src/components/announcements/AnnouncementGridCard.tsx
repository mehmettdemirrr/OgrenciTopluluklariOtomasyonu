import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { Box, CardMedia, Stack, Typography, alpha, useMediaQuery } from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'
import { announcementExcerpt, formatAnnouncementDateTime } from '../../utils/announcementFormat'

export interface AnnouncementGridCardProps {
  /** Verilirse kart tıklanabilir olur ve "Devamını oku" satırı çizilir. Panelde duyuru detay rotası yok — verilmez. */
  to?: string
  title: string
  content: string
  clubName: string | null
  publishedAtUtc: string
  imageFileId: number | null
  /** Panelde görünürlük çipi (Üyelere özel / Herkese açık). */
  badges?: ReactNode
  /** Panelde Düzenle/Kaldır. */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: duyuru liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function AnnouncementGridCard({
  to, title, content, clubName, publishedAtUtc, imageFileId, badges, actions,
}: AnnouncementGridCardProps) {
  const { t, dateLocale } = useLocale()
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)')

  return (
    <Box
      {...(to ? { component: RouterLink, to } : { component: 'div' })}
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        textDecoration: 'none',
        color: 'inherit',
        borderRadius: 2.5,
        overflow: 'hidden',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: (theme) => alpha(theme.palette.secondary.main, 0.08),
        boxShadow: (theme) => `0 4px 20px ${alpha(theme.palette.secondary.main, 0.04)}`,
        transition: 'transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease',
        ...(to
          ? {
              '&:hover': {
                transform: reduceMotion ? 'none' : 'translateY(-8px)',
                borderColor: 'primary.main',
                boxShadow: (theme) => `0 12px 30px ${alpha(theme.palette.secondary.main, 0.08)}`,
              },
            }
          : {}),
      }}
    >
      {imageFileId ? (
        <CardMedia
          component="img"
          image={`/api/files/${imageFileId}`}
          alt=""
          sx={{ height: 200, objectFit: 'cover', borderBottom: '1px solid', borderColor: 'divider' }}
        />
      ) : (
        <Box
          sx={{
            height: 200,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            background: (theme) =>
              `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.1)} 0%, ${alpha(theme.palette.secondary.main, 0.05)} 100%)`,
          }}
        >
          <CampaignOutlinedIcon sx={{ fontSize: 48 }} />
        </Box>
      )}

      <Box sx={{ p: 3, flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Stack direction="row" spacing={1.25} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1.5 }}>
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: 'text.secondary' }}>
            <CalendarMonthOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
            <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
              {formatAnnouncementDateTime(publishedAtUtc, dateLocale)}
            </Typography>
          </Stack>
          {clubName && (
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: 'text.secondary' }}>
              <GroupsOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
              <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                {clubName}
              </Typography>
            </Stack>
          )}
          {badges}
        </Stack>

        <Typography variant="h6" component="h2" sx={{ fontWeight: 800, color: 'text.primary', mb: 1.5, lineHeight: 1.4 }}>
          {title}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ mb: 2.5, lineHeight: 1.6, display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
        >
          {announcementExcerpt(content)}
        </Typography>

        {to && (
          <Stack direction="row" spacing={1} sx={{ mt: 'auto', alignItems: 'center', color: 'primary.main' }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>
              {t('public.readMore')}
            </Typography>
            <ArrowForwardRoundedIcon sx={{ fontSize: 18 }} />
          </Stack>
        )}

        {actions && (
          <Stack direction="row" spacing={1} sx={{ mt: 'auto', pt: 2 }}>
            {actions}
          </Stack>
        )}
      </Box>
    </Box>
  )
}
