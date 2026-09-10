import ChevronLeftRoundedIcon from '@mui/icons-material/ChevronLeftRounded'
import ChevronRightRoundedIcon from '@mui/icons-material/ChevronRightRounded'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import {
  Box,
  Button,
  IconButton,
  Skeleton,
  Stack,
  Typography,
  alpha,
  useMediaQuery,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import {
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  isToday,
  startOfMonth,
  startOfWeek,
} from 'date-fns'
import { enUS, tr } from 'date-fns/locale'
import { useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import type { PublicCalendarEventDto } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import { Permissions } from '../../auth/permissions'
import { EmptyState } from '../ui/EmptyState'
import { useLocale } from '../../i18n/LocaleContext'

function monthRangeUtc(cursor: Date) {
  const from = new Date(Date.UTC(cursor.getFullYear(), cursor.getMonth(), 1))
  const to = new Date(Date.UTC(cursor.getFullYear(), cursor.getMonth() + 1, 1))
  return { fromUtc: from.toISOString(), toUtc: to.toISOString() }
}

function dayKey(date: Date) {
  return format(date, 'yyyy-MM-dd')
}

function eventDayKey(iso: string) {
  return format(new Date(iso), 'yyyy-MM-dd')
}

function eventHref(event: PublicCalendarEventDto, isAuthenticated: boolean) {
  if (event.locked || event.id == null) {
    return null
  }
  // Üyelere özel ama kullanıcıya açılmış etkinlikler vitrin detayında 404 olur.
  return isAuthenticated ? `/events/${event.id}` : `/etkinlikler/${event.id}`
}

export function HomeEventsCalendar() {
  const { t, locale, dateLocale } = useLocale()
  const { isAuthenticated, hasPermission } = useAuth()
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)')
  const [cursor, setCursor] = useState(() => startOfMonth(new Date()))
  const [selected, setSelected] = useState(() => new Date())

  const range = useMemo(() => monthRangeUtc(cursor), [cursor])
  const useAuthCalendar = isAuthenticated && hasPermission(Permissions.EventsRead)
  const dateFnsLocale = locale === 'tr' ? tr : enUS

  const calendarQuery = useQuery({
    queryKey: ['home-calendar', useAuthCalendar ? 'auth' : 'public', range.fromUtc, range.toUtc],
    queryFn: async () => {
      const path = useAuthCalendar ? '/events/calendar' : '/public/calendar-events'
      return (
        await apiClient.get<PublicCalendarEventDto[]>(path, {
          params: { fromUtc: range.fromUtc, toUtc: range.toUtc },
        })
      ).data
    },
  })

  const events = calendarQuery.data ?? []

  const byDay = useMemo(() => {
    const map = new Map<string, PublicCalendarEventDto[]>()
    for (const event of events) {
      const key = eventDayKey(event.startDateUtc)
      const list = map.get(key)
      if (list) {
        list.push(event)
      } else {
        map.set(key, [event])
      }
    }
    for (const list of map.values()) {
      list.sort((a, b) => a.startDateUtc.localeCompare(b.startDateUtc))
    }
    return map
  }, [events])

  const gridDays = useMemo(() => {
    const start = startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 })
    const end = endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 })
    return eachDayOfInterval({ start, end })
  }, [cursor])

  const weekdayLabels = useMemo(() => {
    const base = startOfWeek(new Date(), { weekStartsOn: 1 })
    return Array.from({ length: 7 }, (_, index) =>
      format(new Date(base.getFullYear(), base.getMonth(), base.getDate() + index), 'EEE', { locale: dateFnsLocale }),
    )
  }, [dateFnsLocale])

  const selectedEvents = byDay.get(dayKey(selected)) ?? []
  const monthLabel = format(cursor, 'LLLL yyyy', { locale: dateFnsLocale })

  const goMonth = (delta: number) => {
    const next = addMonths(cursor, delta)
    setCursor(next)
    const today = new Date()
    setSelected(isSameMonth(today, next) ? today : startOfMonth(next))
  }

  const goToday = () => {
    const today = new Date()
    setCursor(startOfMonth(today))
    setSelected(today)
  }

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.4fr) minmax(280px, 0.9fr)' },
        gap: { xs: 2.5, md: 3 },
        alignItems: 'stretch',
      }}
    >
      <Box
        sx={{
          borderRadius: 3,
          border: '1px solid',
          borderColor: (theme) => alpha(theme.palette.secondary.main, 0.1),
          bgcolor: 'background.paper',
          overflow: 'hidden',
          boxShadow: (theme) => `0 16px 40px ${alpha(theme.palette.secondary.main, 0.05)}`,
        }}
      >
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          sx={{
            alignItems: { sm: 'center' },
            justifyContent: 'space-between',
            px: { xs: 2, md: 2.5 },
            py: 2,
            background: (theme) =>
              `linear-gradient(120deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.1)} 100%)`,
            borderBottom: '1px solid',
            borderColor: 'divider',
          }}
        >
          <Box>
            <Typography variant="h6" sx={{ fontWeight: 800, textTransform: 'capitalize', lineHeight: 1.2 }}>
              {monthLabel}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {t('home.calendarLead')}
            </Typography>
          </Box>
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Button size="small" variant="outlined" onClick={goToday} sx={{ fontWeight: 700 }}>
              {t('home.calendarToday')}
            </Button>
            <IconButton size="small" onClick={() => goMonth(-1)} aria-label={t('home.calendarPrev')}>
              <ChevronLeftRoundedIcon />
            </IconButton>
            <IconButton size="small" onClick={() => goMonth(1)} aria-label={t('home.calendarNext')}>
              <ChevronRightRoundedIcon />
            </IconButton>
          </Stack>
        </Stack>

        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, minmax(0, 1fr))', gap: 0.5, px: 1, pt: 1.25 }}>
          {weekdayLabels.map((label) => (
            <Typography
              key={label}
              variant="caption"
              sx={{
                textAlign: 'center',
                fontWeight: 800,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                color: 'text.secondary',
                py: 0.75,
              }}
            >
              {label}
            </Typography>
          ))}
        </Box>

        {calendarQuery.isLoading ? (
          <Box sx={{ p: 2 }}>
            <Skeleton variant="rounded" height={320} />
          </Box>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: 'repeat(7, minmax(0, 1fr))',
              gap: 0.5,
              p: 1,
              pb: 1.5,
            }}
          >
            {gridDays.map((day) => {
              const inMonth = isSameMonth(day, cursor)
              const selectedDay = isSameDay(day, selected)
              const today = isToday(day)
              const dayEvents = byDay.get(dayKey(day)) ?? []
              const visible = dayEvents.slice(0, 2)
              const overflow = dayEvents.length - visible.length

              return (
                <Box
                  key={day.toISOString()}
                  component="button"
                  type="button"
                  onClick={() => setSelected(day)}
                  sx={{
                    minHeight: { xs: 72, md: 96 },
                    p: 0.75,
                    borderRadius: 2,
                    border: '1px solid',
                    borderColor: selectedDay
                      ? 'primary.main'
                      : today
                        ? (theme) => alpha(theme.palette.primary.main, 0.35)
                        : 'transparent',
                    bgcolor: selectedDay
                      ? (theme) => alpha(theme.palette.primary.main, 0.1)
                      : inMonth
                        ? 'background.default'
                        : 'transparent',
                    opacity: inMonth ? 1 : 0.42,
                    cursor: 'pointer',
                    textAlign: 'left',
                    transition: reduceMotion ? 'none' : 'border-color 160ms ease, background-color 160ms ease, transform 160ms ease',
                    '&:hover': {
                      borderColor: (theme) => alpha(theme.palette.primary.main, 0.45),
                      bgcolor: (theme) => alpha(theme.palette.primary.main, 0.06),
                      transform: reduceMotion ? 'none' : 'translateY(-1px)',
                    },
                  }}
                >
                  <Typography
                    variant="caption"
                    sx={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      minWidth: 24,
                      height: 24,
                      px: 0.75,
                      borderRadius: 999,
                      fontWeight: 800,
                      mb: 0.5,
                      color: today || selectedDay ? 'common.white' : 'text.primary',
                      bgcolor: today || selectedDay ? 'primary.main' : 'transparent',
                    }}
                  >
                    {format(day, 'd')}
                  </Typography>
                  <Stack spacing={0.4}>
                    {visible.map((event, index) => (
                      <Box
                        key={event.locked ? `locked-${event.startDateUtc}-${index}` : `open-${event.id}-${index}`}
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 0.4,
                          px: 0.6,
                          py: 0.25,
                          borderRadius: 1,
                          border: '1px solid',
                          borderStyle: event.locked ? 'dashed' : 'solid',
                          borderColor: event.locked
                            ? (theme) => alpha(theme.palette.warning.main, 0.35)
                            : (theme) => alpha(theme.palette.primary.main, 0.28),
                          bgcolor: event.locked
                            ? (theme) => alpha(theme.palette.warning.main, 0.1)
                            : (theme) => alpha(theme.palette.primary.main, 0.12),
                          color: event.locked ? 'warning.main' : 'primary.dark',
                        }}
                      >
                        {event.locked && <LockOutlinedIcon sx={{ fontSize: 11, flexShrink: 0 }} />}
                        <Typography
                          variant="caption"
                          sx={{
                            fontWeight: 700,
                            fontSize: 10,
                            lineHeight: 1.2,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                          }}
                        >
                          {event.locked ? t('home.calendarLocked') : event.title}
                        </Typography>
                      </Box>
                    ))}
                    {overflow > 0 && (
                      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, px: 0.25 }}>
                        {t('home.calendarMore', { count: overflow })}
                      </Typography>
                    )}
                  </Stack>
                </Box>
              )
            })}
          </Box>
        )}

        <Stack
          direction="row"
          spacing={2}
          useFlexGap
          sx={{ flexWrap: 'wrap', px: 2.5, pb: 2, pt: 0.5, color: 'text.secondary' }}
        >
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Box sx={{ width: 10, height: 10, borderRadius: 0.5, bgcolor: 'primary.main' }} />
            <Typography variant="caption" sx={{ fontWeight: 700 }}>
              {t('home.calendarOpenLegend')}
            </Typography>
          </Stack>
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Box
              sx={{
                width: 10,
                height: 10,
                borderRadius: 0.5,
                border: '1px dashed',
                borderColor: 'warning.main',
                bgcolor: (theme) => alpha(theme.palette.warning.main, 0.2),
              }}
            />
            <Typography variant="caption" sx={{ fontWeight: 700 }}>
              {t('home.calendarLockedLegend')}
            </Typography>
          </Stack>
        </Stack>
      </Box>

      <Box
        sx={{
          borderRadius: 3,
          border: '1px solid',
          borderColor: (theme) => alpha(theme.palette.secondary.main, 0.1),
          bgcolor: 'background.paper',
          p: { xs: 2, md: 2.5 },
          minHeight: { lg: 420 },
          boxShadow: (theme) => `0 16px 40px ${alpha(theme.palette.secondary.main, 0.05)}`,
        }}
      >
        <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800, letterSpacing: '0.08em' }}>
          {t('home.calendarSelected')}
        </Typography>
        <Typography variant="h6" sx={{ fontWeight: 800, mb: 2, textTransform: 'capitalize' }}>
          {format(selected, 'd MMMM yyyy, EEEE', { locale: dateFnsLocale })}
        </Typography>

        {calendarQuery.isLoading ? (
          <Stack spacing={1.25}>
            <Skeleton variant="rounded" height={72} />
            <Skeleton variant="rounded" height={72} />
          </Stack>
        ) : selectedEvents.length === 0 ? (
          events.length === 0 ? (
            <EmptyState icon={EventOutlinedIcon} title={t('home.calendarNoMonth')} />
          ) : (
            <EmptyState icon={EventOutlinedIcon} title={t('home.calendarEmptyDay')} />
          )
        ) : (
          <Stack spacing={1.25}>
            {selectedEvents.map((event, index) => {
              const href = eventHref(event, isAuthenticated)
              const timeLabel = new Date(event.startDateUtc).toLocaleTimeString(dateLocale, {
                hour: '2-digit',
                minute: '2-digit',
              })

              const body = (
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                  <Box
                    sx={{
                      width: 40,
                      height: 40,
                      borderRadius: 2,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      flexShrink: 0,
                      bgcolor: event.locked
                        ? (theme) => alpha(theme.palette.warning.main, 0.14)
                        : (theme) => alpha(theme.palette.primary.main, 0.14),
                      color: event.locked ? 'warning.main' : 'primary.main',
                    }}
                  >
                    {event.locked ? <LockOutlinedIcon fontSize="small" /> : <EventOutlinedIcon fontSize="small" />}
                  </Box>
                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
                      {event.locked ? t('home.calendarLocked') : event.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.25 }}>
                      {timeLabel}
                      {!event.locked && event.clubName ? ` · ${event.clubName}` : ''}
                    </Typography>
                    {event.locked ? (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75, lineHeight: 1.5 }}>
                        {t('home.calendarLockedHint')}
                      </Typography>
                    ) : (
                      event.location && (
                        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', mt: 0.75, color: 'text.secondary' }}>
                          <PlaceOutlinedIcon sx={{ fontSize: 14 }} />
                          <Typography variant="caption" noWrap>
                            {event.location}
                          </Typography>
                        </Stack>
                      )
                    )}
                  </Box>
                </Stack>
              )

              return href ? (
                <Box
                  key={`open-${event.id}`}
                  component={RouterLink}
                  to={href}
                  sx={{
                    display: 'block',
                    p: 1.5,
                    borderRadius: 2,
                    textDecoration: 'none',
                    color: 'inherit',
                    border: '1px solid',
                    borderColor: (theme) => alpha(theme.palette.primary.main, 0.2),
                    bgcolor: (theme) => alpha(theme.palette.primary.main, 0.04),
                    transition: reduceMotion ? 'none' : 'border-color 160ms ease, box-shadow 160ms ease',
                    '&:hover': {
                      borderColor: 'primary.main',
                      boxShadow: (theme) => `0 10px 24px ${alpha(theme.palette.secondary.main, 0.08)}`,
                    },
                  }}
                >
                  {body}
                </Box>
              ) : (
                <Box
                  key={`locked-${event.startDateUtc}-${index}`}
                  sx={{
                    p: 1.5,
                    borderRadius: 2,
                    border: '1px dashed',
                    borderColor: (theme) => alpha(theme.palette.warning.main, 0.4),
                    bgcolor: (theme) => alpha(theme.palette.warning.main, 0.06),
                  }}
                >
                  {body}
                </Box>
              )
            })}
          </Stack>
        )}
      </Box>
    </Box>
  )
}
