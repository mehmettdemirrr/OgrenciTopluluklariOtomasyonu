import { Box, Typography, alpha } from '@mui/material'
import { useLocale } from '../../i18n/LocaleContext'

export function DateBadge({ iso }: { iso: string }) {
  const { dateLocale } = useLocale()
  const date = new Date(iso)
  const day = date.toLocaleDateString(dateLocale, { day: '2-digit' })
  const month = date.toLocaleDateString(dateLocale, { month: 'short' })

  return (
    <Box
      sx={{
        minWidth: 56,
        px: 1,
        py: 0.75,
        borderRadius: 2,
        textAlign: 'center',
        bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12),
        color: 'primary.dark',
        flexShrink: 0,
      }}
    >
      <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.1 }}>
        {day}
      </Typography>
      <Typography variant="caption" sx={{ textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700 }}>
        {month}
      </Typography>
    </Box>
  )
}
