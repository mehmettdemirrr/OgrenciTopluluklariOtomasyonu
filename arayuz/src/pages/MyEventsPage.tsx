import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Card, CardActions, CardContent, Chip, Grid, Stack, Typography } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { EventStatusChip } from '../components/ui/StatusChip'
import type { EventListItemDto, PagedResult } from '../api/types'

export function MyEventsPage() {
  const queryClient = useQueryClient()
  const notify = useNotifier()

  const { query: mineQuery } = usePagedQuery({
    queryKey: ['events-mine-page'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<EventListItemDto>>('/events/mine', { params: { pageIndex, pageSize } })).data,
  })

  const cancelMutation = useMutation({
    mutationFn: async (eventId: number) => {
      await apiClient.delete(`/events/${eventId}/participation`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik kaydınız iptal edildi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events-mine-page'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt iptal edilemedi.'), severity: 'error' }),
  })

  const items = mineQuery.data?.items ?? []

  return (
    <>
      <PageHeader title="Etkinliklerim" description="Kayıt olduğunuz etkinlikler." />

      {!mineQuery.isLoading && items.length === 0 ? (
        <EmptyState icon={EventOutlinedIcon} title="Kayıtlı olduğunuz bir etkinlik yok" description="Etkinlikler sayfasından yaklaşan etkinliklere katılabilirsiniz." />
      ) : (
        <Grid container spacing={2}>
          {items.map((event) => (
            <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <CardContent sx={{ flex: 1 }}>
                  <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
                      {event.title}
                    </Typography>
                    <EventStatusChip status={event.status} />
                  </Stack>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                    {event.clubName}
                  </Typography>
                  <Typography variant="body2" sx={{ mb: 0.5 }}>
                    {new Date(event.startDateUtc).toLocaleString('tr-TR')}
                  </Typography>
                  {event.location && (
                    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'text.secondary' }}>
                      <PlaceOutlinedIcon fontSize="inherit" />
                      <Typography variant="caption">{event.location}</Typography>
                    </Stack>
                  )}
                  {event.capacity && <Chip size="small" sx={{ mt: 1 }} label={`Kontenjan: ${event.capacity}`} variant="outlined" />}
                </CardContent>
                <CardActions sx={{ px: 2, pb: 2, gap: 0.5 }}>
                  <Button size="small" component={RouterLink} to={`/events/${event.id}`}>
                    Detay
                  </Button>
                  <Button size="small" color="error" disabled={cancelMutation.isPending} onClick={() => cancelMutation.mutate(event.id)}>
                    Ayrıl
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </>
  )
}
