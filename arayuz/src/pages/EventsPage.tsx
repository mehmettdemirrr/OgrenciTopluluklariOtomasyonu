import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  Grid,
  Radio,
  RadioGroup,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useSearchPagedQuery } from '../hooks/useSearchPagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { CardGridSkeleton } from '../components/ui/CardGridSkeleton'
import { DateBadge } from '../components/ui/DateBadge'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { ResultPagination } from '../components/ui/ResultPagination'
import { SearchField } from '../components/ui/SearchField'
import { EventAudienceChip, EventStatusChip } from '../components/ui/StatusChip'
import { emptyEventFormValues, eventFormSchema, toEventPayload, type EventFormValues } from '../schemas/eventForm'
import type { ClubListItemDto, EventListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

export function EventsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('pages.events'))

  const { hasPermission } = useAuth()
  const [tab, setTab] = useState(0)
  const canApprove = hasPermission(Permissions.EventsApprove)

  return (
    <>
      <PageHeader title={t('pages.events')} description={t('pages.eventsLead')} />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label={t('pages.upcoming')} />
        <Tab label={t('pages.myClubEvents')} />
        {canApprove && <Tab label={t('pages.approvalQueue')} />}
      </Tabs>

      {tab === 0 && <UpcomingTab />}
      {tab === 1 && <EventsTab />}
      {tab === 2 && canApprove && <ApprovalQueueTab />}
    </>
  )
}

function UpcomingTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()

  // A-50/Y-62: arama ve sayfalama sunucuda; "kayıtlı mıyım" bilgisi listeyle birlikte geliyor
  // (isRegistered), dolayısıyla ikinci bir tam /events/mine çekimine gerek kalmadı.
  const { search, setSearch, items, pageIndex, setPageIndex, pageCount, totalCount, query: upcomingQuery } =
    useSearchPagedQuery<EventListItemDto>({
      queryKey: ['events-upcoming'],
      queryFn: async ({ pageIndex: page, pageSize, search: term }) =>
        (await apiClient.get<PagedResult<EventListItemDto>>('/events/upcoming', {
          params: { pageIndex: page, pageSize, search: term || undefined },
        })).data,
    })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['events-upcoming'] })
    queryClient.invalidateQueries({ queryKey: ['events-mine'] })
  }

  const registerMutation = useMutation({
    mutationFn: async (eventId: number) => {
      await apiClient.post(`/events/${eventId}/participation`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinliğe kaydınız alındı.', severity: 'success' })
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt oluşturulamadı.'), severity: 'error' }),
  })

  const cancelMutation = useMutation({
    mutationFn: async (eventId: number) => {
      await apiClient.delete(`/events/${eventId}/participation`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik kaydınız iptal edildi.', severity: 'success' })
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt iptal edilemedi.'), severity: 'error' }),
  })

  return (
    <>
      <Stack sx={{ mb: 3 }}>
        <SearchField value={search} onChange={setSearch} placeholder="Etkinlik ara…" />
      </Stack>

      {upcomingQuery.isLoading && <CardGridSkeleton />}

      {!upcomingQuery.isLoading && items.length === 0 && (
        <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" description="Şu anda yayında ve başlamamış bir etkinlik bulunmuyor." />
      )}

      <Grid container spacing={2}>
        {items.map((event) => {
          const isRegistered = event.isRegistered === true
          return (
            <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <CardContent sx={{ flex: 1 }}>
                  <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 1.25 }}>
                    <DateBadge iso={event.startDateUtc} />
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 1, mb: 0.5 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 800 }} noWrap>
                          {event.title}
                        </Typography>
                        <Chip size="small" label={event.capacity ? `Kontenjan: ${event.capacity}` : 'Sınırsız'} variant="outlined" />
                      </Stack>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
                        <Typography variant="body2" color="text.secondary" noWrap>
                          {event.clubName}
                        </Typography>
                        {/* K-38: "Katıl" düğmesine basmadan önce üyelik şartını görsün. */}
                        {event.audience === 'ClubMembers' && <EventAudienceChip audience={event.audience} />}
                      </Stack>
                      <Typography variant="body2" sx={{ mb: 0.5 }}>
                        {new Date(event.startDateUtc).toLocaleString('tr-TR')}
                      </Typography>
                      {event.location && (
                        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'text.secondary' }}>
                          <PlaceOutlinedIcon fontSize="inherit" />
                          <Typography variant="caption">{event.location}</Typography>
                        </Stack>
                      )}
                    </Box>
                  </Stack>
                </CardContent>
                <CardActions sx={{ px: 2, pb: 2, gap: 0.5 }}>
                  <Button size="small" component={RouterLink} to={`/events/${event.id}`}>
                    Detay
                  </Button>
                  {isRegistered ? (
                    <Button size="small" color="error" disabled={cancelMutation.isPending} onClick={() => cancelMutation.mutate(event.id)}>
                      Ayrıl
                    </Button>
                  ) : (
                    <Button size="small" variant="outlined" disabled={registerMutation.isPending} onClick={() => registerMutation.mutate(event.id)}>
                      Katıl
                    </Button>
                  )}
                </CardActions>
              </Card>
            </Grid>
          )
        })}
      </Grid>

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}

function EventsTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.EventsWrite)
  const [selectedClubId, setSelectedClubId] = useState<number | ''>('')
  const createDialog = useFormDialog()

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting: isFormSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventFormSchema),
    defaultValues: emptyEventFormValues,
  })

  const { paginationModel, setPaginationModel, query: eventsQuery } = usePagedQuery({
    queryKey: ['club-events', selectedClubId],
    enabled: selectedClubId !== '' && canWrite,
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<EventListItemDto>>(`/clubs/${selectedClubId}/events`, { params: { pageIndex, pageSize } })).data,
  })

  const createEventMutation = useMutation({
    mutationFn: async (values: EventFormValues) => {
      if (selectedClubId === '') throw new Error('Önce bir topluluk seçin.')
      await apiClient.post(`/clubs/${selectedClubId}/events`, toEventPayload(values))
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      createDialog.closeDialog()
      reset(emptyEventFormValues)
      queryClient.invalidateQueries({ queryKey: ['club-events'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik oluşturulamadı.'), severity: 'error' }),
  })

  const submitMutation = useMutation({
    mutationFn: async (eventId: number) => {
      await apiClient.put(`/events/${eventId}/submission`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik onaya gönderildi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Onaya gönderilemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<EventListItemDto>[] = [
    { field: 'title', headerName: 'Başlık', flex: 1, minWidth: 200 },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    { field: 'status', headerName: 'Durum', width: 150, renderCell: (params) => <EventStatusChip status={params.row.status} /> },
    { field: 'audience', headerName: 'Kitle', width: 130, renderCell: (params) => <EventAudienceChip audience={params.row.audience} /> },
    {
      field: 'actions',
      headerName: '',
      width: 220,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button size="small" component={RouterLink} to={`/events/${params.row.id}`}>
            Detay
          </Button>
          {canWrite && (
            <Button
              size="small"
              variant="outlined"
              disabled={params.row.status !== 'Draft' || submitMutation.isPending}
              onClick={() => submitMutation.mutate(params.row.id)}
            >
              Onaya Gönder
            </Button>
          )}
        </Stack>
      ),
    },
  ]

  return (
    <>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 2 }}>
        <Stack sx={{ width: 320 }}>
          <RemoteSelect<ClubListItemDto>
            label="Topluluk"
            size="small"
            value={selectedClubId === '' ? null : selectedClubId}
            onChange={(value) => setSelectedClubId(value ?? '')}
            queryKey={['clubs', 'selector']}
            fetchOptions={async (term) =>
              (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
                params: { pageIndex: 0, pageSize: 20, search: term || undefined, isActive: true },
              })).data.items
            }
            getOptionId={(club) => club.id}
            getOptionLabel={(club) => club.name}
          />
        </Stack>

        {canWrite && (
          <Button variant="contained" disabled={selectedClubId === ''} onClick={createDialog.openDialog}>
            Etkinlik Oluştur
          </Button>
        )}
      </Stack>

      {selectedClubId === '' ? (
        <EmptyState
          icon={EventOutlinedIcon}
          title="Bir topluluk seçin"
          description="Etkinlikleri görüntülemek için önce yukarıdan bir topluluk seçin."
        />
      ) : !canWrite ? (
        <EmptyState
          icon={EventOutlinedIcon}
          title="Yönetim yetkiniz yok"
          description="Bu topluluğun etkinliklerini yönetmek için danışman veya yetkili/başkan olmanız gerekir. Yayındaki etkinlikleri Yaklaşan Etkinlikler sekmesinden görebilirsiniz."
        />
      ) : (
        <DataTable
          mobileHiddenFields={['startDateUtc', 'audience']}
          rows={eventsQuery.data?.items ?? []}
          columns={columns}
          loading={eventsQuery.isFetching}
          paginationMode="server"
          rowCount={eventsQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          emptyTitle="Bu toplulukta etkinlik yok"
        />
      )}

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          reset(emptyEventFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Etkinlik</DialogTitle>
        <DialogContent>
          <Controller
            name="title"
            control={control}
            render={({ field, fieldState }) => (
              <TextField {...field} autoFocus fullWidth margin="dense" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="description"
            control={control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={2} margin="dense" label="Açıklama" />}
          />
          <Controller name="location" control={control} render={({ field }) => <TextField {...field} fullWidth margin="dense" label="Yer" />} />
          <Controller
            name="startDateTime"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                margin="dense"
                label="Başlangıç"
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
          <Controller
            name="endDateTime"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                margin="dense"
                label="Bitiş"
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
          <Controller
            name="capacity"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                margin="dense"
                label="Kontenjan (boş = sınırsız)"
                type="number"
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
          <Controller
            name="audience"
            control={control}
            render={({ field }) => (
              <FormControl margin="dense">
                <FormLabel>Kimler katılabilir?</FormLabel>
                <RadioGroup {...field} row>
                  <FormControlLabel value="Public" control={<Radio />} label="Herkese açık" />
                  <FormControlLabel value="ClubMembers" control={<Radio />} label="Sadece topluluk üyeleri" />
                </RadioGroup>
              </FormControl>
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              reset(emptyEventFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={isFormSubmitting || createEventMutation.isPending}
            onClick={handleSubmit((values) => createEventMutation.mutate(values))}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

function ApprovalQueueTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()

  const { paginationModel, setPaginationModel, query: queueQuery } = usePagedQuery({
    queryKey: ['events-approval-queue'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<EventListItemDto>>('/events/approval-queue', { params: { pageIndex, pageSize } })).data,
  })

  const decideMutation = useMutation({
    mutationFn: async ({ id, status }: { id: number; status: 'Published' | 'Rejected' }) => {
      await apiClient.put(`/events/${id}/decision`, { status })
    },
    onSuccess: (_data, variables) => {
      notify({ message: variables.status === 'Published' ? 'Etkinlik yayınlandı.' : 'Etkinlik reddedildi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events-approval-queue'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<EventListItemDto>[] = [
    { field: 'clubName', headerName: 'Topluluk', flex: 1, minWidth: 160 },
    { field: 'title', headerName: 'Başlık', flex: 1, minWidth: 200 },
    // K-38: onaylayan danışman etkinliğin kime açık olduğunu KARAR VERMEDEN ÖNCE görmeli.
    { field: 'audience', headerName: 'Kitle', width: 130, renderCell: (params) => <EventAudienceChip audience={params.row.audience} /> },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    {
      field: 'actions',
      headerName: '',
      width: 220,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button
            size="small"
            variant="contained"
            color="success"
            disabled={decideMutation.isPending}
            onClick={() => decideMutation.mutate({ id: params.row.id, status: 'Published' })}
          >
            Onayla
          </Button>
          <Button
            size="small"
            variant="outlined"
            color="error"
            disabled={decideMutation.isPending}
            onClick={() => decideMutation.mutate({ id: params.row.id, status: 'Rejected' })}
          >
            Reddet
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <DataTable
      mobileHiddenFields={['clubName', 'startDateUtc', 'audience']}
      rows={queueQuery.data?.items ?? []}
      columns={columns}
      loading={queueQuery.isFetching}
      paginationMode="server"
      rowCount={queueQuery.data?.totalCount ?? 0}
      paginationModel={paginationModel}
      onPaginationModelChange={setPaginationModel}
      pageSizeOptions={[10, 20, 50]}
      emptyTitle="Onay bekleyen etkinlik yok"
    />
  )
}
