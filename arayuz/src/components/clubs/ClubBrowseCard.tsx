import { Box, Button, Card, CardContent, Chip, IconButton, Stack, Tooltip, Typography, alpha } from '@mui/material'
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import ShareOutlinedIcon from '@mui/icons-material/ShareOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'
import type { PublicClubListItemDto } from '../../api/types'

interface ClubBrowseCardProps {
  club: PublicClubListItemDto
  isAuthenticated: boolean
  joining: boolean
  onJoin: (clubId: number) => void
  onShare: (club: PublicClubListItemDto) => void
}

/** docs/MIMARI.md · K-50: vitrin kartı. Y-86: "Katıl" uygunluk kararı vermez, ucu çağırır. */
export function ClubBrowseCard({ club, isAuthenticated, joining, onJoin, onShare }: ClubBrowseCardProps) {
  const { t } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3, position: 'relative' }}>
      <Tooltip title={t('public.shareClub')}>
        <IconButton
          size="small"
          aria-label={t('public.shareClub')}
          onClick={() => onShare(club)}
          sx={{ position: 'absolute', top: 8, left: 8, zIndex: 1 }}
        >
          <ShareOutlinedIcon fontSize="small" />
        </IconButton>
      </Tooltip>

      <Stack
        direction="row"
        spacing={0.5}
        useFlexGap
        sx={{ position: 'absolute', top: 8, right: 8, zIndex: 1, flexWrap: 'wrap', justifyContent: 'flex-end', maxWidth: '65%' }}
      >
        {club.clubCategoryNames.map((name) => (
          <Chip key={name} size="small" label={name} sx={{ bgcolor: 'text.primary', color: 'background.paper', fontWeight: 700 }} />
        ))}
      </Stack>

      <Box
        sx={{
          height: 170,
          mt: 4,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 3,
          background: (theme) => (club.logoFileId ? 'transparent' : `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.12)} 100%)`),
        }}
      >
        {club.logoFileId ? (
          <Box component="img" src={`/api/files/${club.logoFileId}`} alt="" sx={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }} />
        ) : (
          <GroupsOutlinedIcon sx={{ fontSize: 56, color: 'primary.dark' }} />
        )}
      </Box>

      <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 800, textTransform: 'uppercase', mb: 1 }}>
          {club.name}
        </Typography>

        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
          <Chip size="small" icon={<GroupsOutlinedIcon />} label={t('public.memberCount', { count: club.memberCount })} />
          <Chip size="small" icon={<EventAvailableOutlinedIcon />} label={t('public.eventCount', { count: club.eventCount })} />
        </Stack>

        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', mb: 2, minHeight: 40 }}
        >
          {club.description || t('common.noDescription')}
        </Typography>

        <Stack spacing={1} sx={{ mt: 'auto' }}>
          {isAuthenticated ? (
            <Button variant="contained" color="success" disabled={joining} onClick={() => onJoin(club.id)}>
              {t('public.join')}
            </Button>
          ) : (
            <Button variant="contained" color="success" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
              {t('public.loginAndJoin')}
            </Button>
          )}
          <Button variant="outlined" component={RouterLink} to={`/kulupler/${club.id}`}>
            {t('public.inspect')}
          </Button>
        </Stack>
      </CardContent>
    </Card>
  )
}
