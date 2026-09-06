import { useQuery } from '@tanstack/react-query'
import { Button, Card, CardContent, Skeleton, Stack, Typography } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import { useEffect } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import { BackButton } from '../../components/ui/BackButton'
import { EmptyState } from '../../components/ui/EmptyState'
import { EventDetailLayout } from '../../components/events/EventDetailLayout'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import type { PublicEventDetailDto } from '../../api/types'

export function PublicEventDetailPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const eventId = Number(id)
  const { isAuthenticated } = useAuth()

  const eventQuery = useQuery({
    queryKey: ['public-event', eventId],
    queryFn: async () => (await apiClient.get<PublicEventDetailDto>(`/public/events/${eventId}`)).data,
  })

  useDocumentTitle(eventQuery.data?.title)

  // docs/MIMARI.md · A-77: sayaç ayrı uçta ve oturum başına bir kez; hata yutulur, sayfa etkilenmez.
  useEffect(() => {
    if (!eventQuery.isSuccess) {
      return
    }
    const key = `event-view-${eventId}`
    if (sessionStorage.getItem(key)) {
      return
    }
    sessionStorage.setItem(key, '1')
    apiClient.post(`/public/events/${eventId}/view`).catch(() => undefined)
  }, [eventQuery.isSuccess, eventId])

  if (eventQuery.isLoading) {
    return (
      <Stack spacing={2}>
        <BackButton to="/etkinlikler" />
        <Skeleton variant="rounded" height={360} />
      </Stack>
    )
  }

  if (eventQuery.isError || !eventQuery.data) {
    return (
      <Stack spacing={2}>
        <BackButton to="/etkinlikler" />
        <EmptyState icon={EventOutlinedIcon} title={t('public.eventMissing')} description={t('public.eventMissingLead')} />
      </Stack>
    )
  }

  const event = eventQuery.data

  return (
    <Stack spacing={3}>
      <BackButton to="/etkinlikler" />
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        {event.title}
      </Typography>

      <EventDetailLayout
        event={event}
        clubHref={`/kulupler/${event.clubId}`}
        primaryAction={
          isAuthenticated ? (
            <Button fullWidth size="large" variant="contained" component={RouterLink} to={`/events/${event.id}`}>
              {t('common.goPanel')}
            </Button>
          ) : (
            <Button fullWidth size="large" variant="contained" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
              {t('event.loginToJoin')}
            </Button>
          )
        }
        actionNote={
          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent sx={{ py: 1.5 }}>
              <Typography variant="caption" color="text.secondary">
                {t('public.loginToRegisterHint')}
              </Typography>
            </CardContent>
          </Card>
        }
      />
    </Stack>
  )
}
