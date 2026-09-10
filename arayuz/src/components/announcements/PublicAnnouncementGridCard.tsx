import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { Box, CardMedia, Stack, Typography, alpha, useMediaQuery } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import type { PublicAnnouncementListItemDto } from '../../api/types'
import { useLocale } from '../../i18n/LocaleContext'
import { announcementExcerpt, formatAnnouncementDateTime } from '../../utils/announcementFormat'

export function PublicAnnouncementGridCard({ announcement }: { announcement: PublicAnnouncementListItemDto }) {
  const { t, dateLocale } = useLocale()
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)')

  return (
    <Box
      component={RouterLink}
      to={`/duyurular/${announcement.id}`}
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
        '&:hover': {
          transform: reduceMotion ? 'none' : 'translateY(-8px)',
          borderColor: 'primary.main',
          boxShadow: (theme) => `0 12px 30px ${alpha(theme.palette.secondary.main, 0.08)}`,
        },
      }}
    >
      {announcement.imageFileId ? (
        <CardMedia
          component="img"
          image={`/api/files/${announcement.imageFileId}`}
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
              {formatAnnouncementDateTime(announcement.publishedAtUtc, dateLocale)}
            </Typography>
          </Stack>
          {announcement.clubName && (
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: 'text.secondary' }}>
              <GroupsOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
              <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                {announcement.clubName}
              </Typography>
            </Stack>
          )}
        </Stack>

        <Typography variant="h6" component="h2" sx={{ fontWeight: 800, color: 'text.primary', mb: 1.5, lineHeight: 1.4 }}>
          {announcement.title}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{
            mb: 2.5,
            lineHeight: 1.6,
            display: '-webkit-box',
            WebkitLineClamp: 3,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
          }}
        >
          {announcementExcerpt(announcement.content)}
        </Typography>

        <Stack direction="row" spacing={1} sx={{ mt: 'auto', alignItems: 'center', color: 'primary.main' }}>
          <Typography variant="body2" sx={{ fontWeight: 700 }}>
            {t('public.readMore')}
          </Typography>
          <ArrowForwardRoundedIcon sx={{ fontSize: 18 }} />
        </Stack>
      </Box>
    </Box>
  )
}
