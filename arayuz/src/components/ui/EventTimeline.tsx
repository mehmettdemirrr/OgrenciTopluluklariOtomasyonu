import { useEffect, useState } from 'react'
import { Box, Stack, Typography } from '@mui/material'
import { differenceInCalendarDays, differenceInHours } from 'date-fns'
import ScheduleRoundedIcon from '@mui/icons-material/ScheduleRounded'
import { useLocale } from '../../i18n/LocaleContext'

/**
 * docs/MIMARI.md · Y-79: buradaki kalan süre YALNIZCA bilgilendirir.
 * Katılım/durum kararları backend alanlarından okunur, bu hesaptan değil.
 */
function remainingLabel(startIso: string, endIso: string, now: Date, t: (key: string, vars?: Record<string, string | number>) => string) {
  const start = new Date(startIso)
  const end = new Date(endIso)

  if (now >= end) {
    return t('event.finished')
  }
  if (now >= start) {
    return t('event.ongoing')
  }

  const days = differenceInCalendarDays(start, now)
  if (days > 0) {
    return t('event.daysLeft', { count: days })
  }

  const hours = differenceInHours(start, now)
  return hours > 0 ? t('event.hoursLeft', { count: hours }) : t('event.startingSoon')
}

export function EventTimeline({ startIso, endIso }: { startIso: string; endIso: string }) {
  const { t, dateLocale } = useLocale()
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const interval = window.setInterval(() => setNow(new Date()), 60_000)
    return () => window.clearInterval(interval)
  }, [])

  const formatDateTime = (iso: string) =>
    new Date(iso).toLocaleString(dateLocale, { dateStyle: 'medium', timeStyle: 'short' })

  return (
    <Box sx={{ borderRadius: 2, border: '1px solid', borderColor: 'divider', p: 2 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
        <ScheduleRoundedIcon fontSize="small" color="primary" />
        <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
          {remainingLabel(startIso, endIso, now, t)}
        </Typography>
      </Stack>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={{ xs: 0.5, sm: 3 }}>
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {t('event.start')}
          </Typography>
          <Typography variant="body2">{formatDateTime(startIso)}</Typography>
        </Box>
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {t('event.end')}
          </Typography>
          <Typography variant="body2">{formatDateTime(endIso)}</Typography>
        </Box>
      </Stack>
    </Box>
  )
}
