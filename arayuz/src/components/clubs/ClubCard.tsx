import { Box, Card, CardContent, Chip, IconButton, Stack, Tooltip, Typography, alpha } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import ShareOutlinedIcon from '@mui/icons-material/ShareOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'

export interface ClubCardProps {
  /** Kart başlığının gittiği adres — panelde /clubs/:id, vitrinde /kulupler/:id. */
  to: string
  name: string
  description: string | null
  logoFileId: number | null
  categoryNames: string[]
  /** "N Üye" / "N Etkinlik" çipleri. Panel DTO'su sayı taşımadığı için orada verilmez. */
  stats?: ReactNode
  /** Sayfaya özgü rozet (panelde Aktif/Pasif durumu). */
  badges?: ReactNode
  onShare?: () => void
  primaryAction?: ReactNode
  secondaryActions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: kulüp liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function ClubCard({
  to, name, description, logoFileId, categoryNames, stats, badges, onShare, primaryAction, secondaryActions,
}: ClubCardProps) {
  const { t } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3, position: 'relative' }}>
      {onShare && (
        <Tooltip title={t('public.shareClub')}>
          <IconButton
            size="small"
            aria-label={t('public.shareClub')}
            onClick={onShare}
            sx={{ position: 'absolute', top: 8, left: 8, zIndex: 1 }}
          >
            <ShareOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      )}

      <Stack
        direction="row"
        spacing={0.5}
        useFlexGap
        sx={{ position: 'absolute', top: 8, right: 8, zIndex: 1, flexWrap: 'wrap', justifyContent: 'flex-end', maxWidth: '65%' }}
      >
        {badges}
        {categoryNames.map((categoryName) => (
          <Chip key={categoryName} size="small" label={categoryName} sx={{ bgcolor: 'text.primary', color: 'background.paper', fontWeight: 700 }} />
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
          background: (theme) => (logoFileId ? 'transparent' : `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.12)} 100%)`),
        }}
      >
        {logoFileId ? (
          <Box component="img" src={`/api/files/${logoFileId}`} alt="" sx={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }} />
        ) : (
          <GroupsOutlinedIcon sx={{ fontSize: 56, color: 'primary.dark' }} />
        )}
      </Box>

      <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Typography
          component={RouterLink}
          to={to}
          variant="subtitle1"
          sx={{ fontWeight: 800, textTransform: 'uppercase', mb: 1, display: 'block', textDecoration: 'none', color: 'inherit' }}
        >
          {name}
        </Typography>

        {stats && (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
            {stats}
          </Stack>
        )}

        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', mb: 2, minHeight: 40 }}
        >
          {description || t('common.noDescription')}
        </Typography>

        <Stack spacing={1} sx={{ mt: 'auto' }}>
          {primaryAction}
          {secondaryActions}
        </Stack>
      </CardContent>
    </Card>
  )
}
