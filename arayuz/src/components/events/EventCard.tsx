import { Box, Card, CardContent, CardMedia, Stack, Typography } from '@mui/material'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { DateBadge } from '../ui/DateBadge'
import { useLocale } from '../../i18n/LocaleContext'

export interface EventCardProps {
  /** Başlığın gittiği adres — panelde /events/:id, vitrinde /etkinlikler/:id. */
  to: string
  title: string
  clubName: string
  startDateUtc: string
  location: string | null
  posterFileId: number | null
  /** Kontenjan, kitle (ClubMembers) gibi rozetler — sayfa kendi setini verir (Y-72). */
  badges?: ReactNode
  /** Panelde Detay/Katıl/Ayrıl; vitrinde yok (kartın kendisi bağlantı). */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: etkinlik liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function EventCard({ to, title, clubName, startDateUtc, location, posterFileId, badges, actions }: EventCardProps) {
  const { dateLocale } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3 }}>
      {posterFileId && (
        <CardMedia
          component={RouterLink}
          to={to}
          image={`/api/files/${posterFileId}`}
          sx={{ height: 160, display: 'block' }}
        />
      )}
      <CardContent sx={{ flex: 1 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 1.5 }}>
          <DateBadge iso={startDateUtc} />
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography
              component={RouterLink}
              to={to}
              variant="h6"
              sx={{ fontWeight: 800, fontSize: 18, display: 'block', textDecoration: 'none', color: 'inherit' }}
              noWrap
            >
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              {clubName}
            </Typography>
          </Box>
        </Stack>

        {badges && (
          <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap', mb: 1 }}>
            {badges}
          </Stack>
        )}

        <Typography variant="body2" sx={{ mb: 0.5 }}>
          {new Date(startDateUtc).toLocaleString(dateLocale)}
        </Typography>
        {location && (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'text.secondary' }}>
            <PlaceOutlinedIcon fontSize="inherit" />
            <Typography variant="caption">{location}</Typography>
          </Stack>
        )}

        {actions && (
          <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
            {actions}
          </Stack>
        )}
      </CardContent>
    </Card>
  )
}
