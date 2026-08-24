import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Skeleton, Stack, TextField, Typography } from '@mui/material'
import { z } from 'zod'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import type { GridColDef } from '@mui/x-data-grid'
import { useRef, useState, type ChangeEvent } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate, useParams } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { EventStatusChip } from '../components/ui/StatusChip'
import { emptyEventFormValues, eventFormSchema, toEventPayload, type EventFormValues } from '../schemas/eventForm'
import type { EventListItemDto, EventParticipantListItemDto, PagedResult } from '../api/types'

export function EventDetailPage() {
  const { id } = useParams<{ id: string }>()
  const eventId = Number(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(Permissions.EventsWrite)

  const editDialog = useFormDialog()
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const posterInputRef = useRef<HTMLInputElement>(null)
  const canUploadPoster = hasPermission(Permissions.FilesUpload)

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting: isFormSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventFormSchema),
    defaultValues: emptyEventFormValues,
  })

  const eventQuery = useQuery({
    queryKey: ['events', eventId],
    queryFn: async () => (await apiClient.get<EventListItemDto>(`/events/${eventId}`)).data,
  })

  useDocumentTitle(eventQuery.data?.title)

  // Y-62: önceden bu soru için TÜM /events/mine listesi (200'lük tek sayfa) çekiliyordu.
  const registrationQuery = useQuery({
    queryKey: ['event-registration', eventId],
    queryFn: async () => (await apiClient.get<boolean>(`/events/${eventId}/participation/mine`)).data,
  })

  const isRegistered = registrationQuery.data === true

  const invalidateEvent = () => {
    queryClient.invalidateQueries({ queryKey: ['events', eventId] })
    queryClient.invalidateQueries({ queryKey: ['event-registration', eventId] })
    queryClient.invalidateQueries({ queryKey: ['events-mine'] })
    queryClient.invalidateQueries({ queryKey: ['events-upcoming'] })
  }

  const registerMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post(`/events/${eventId}/participation`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinliğe kaydınız alındı.', severity: 'success' })
      invalidateEvent()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt oluşturulamadı.'), severity: 'error' }),
  })

  const cancelMutation = useMutation({
    mutationFn: async () => {
      await apiClient.delete(`/events/${eventId}/participation`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik kaydınız iptal edildi.', severity: 'success' })
      invalidateEvent()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt iptal edilemedi.'), severity: 'error' }),
  })

  const submitMutation = useMutation({
    mutationFn: async () => {
      await apiClient.put(`/events/${eventId}/submission`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik onaya gönderildi.', severity: 'success' })
      invalidateEvent()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Onaya gönderilemedi.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: EventFormValues) => {
      await apiClient.put(`/events/${eventId}`, toEventPayload(values))
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik güncellendi.', severity: 'success' })
      editDialog.closeDialog()
      invalidateEvent()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik güncellenemedi.'), severity: 'error' }),
  })

  const cancelDialog = useFormDialog()
  const cancelForm = useForm<{ cancellationReason: string }>({
    resolver: zodResolver(z.object({ cancellationReason: z.string().min(1, 'İptal gerekçesi gerekli.') })),
    defaultValues: { cancellationReason: '' },
  })

  // Not: aşağıdaki `cancelMutation` öğrencinin KENDİ KAYDINI iptal etmesi; bu ise etkinliğin
  // tamamının iptali (A-49). İkisi farklı yetki ve farklı uç.
  const cancelEventMutation = useMutation({
    mutationFn: async (values: { cancellationReason: string }) => {
      await apiClient.put(`/events/${eventId}/cancellation`, values)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik iptal edildi, katılımcılara bildirim gönderiliyor.', severity: 'success' })
      cancelDialog.closeDialog()
      cancelForm.reset({ cancellationReason: '' })
      invalidateEvent()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik iptal edilemedi.'), severity: 'error' }),
  })

  const posterMutation = useMutation({
    mutationFn: async (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post(`/events/${eventId}/poster`, formData)
    },
    onSuccess: () => notify({ message: 'Afiş güncellendi.', severity: 'success' }),
    onError: (error) => notify({ message: extractErrorMessage(error, 'Afiş yüklenemedi.'), severity: 'error' }),
  })

  const handlePosterFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (file) {
      posterMutation.mutate(file)
    }
  }

  const deleteMutation = useMutation({
    mutationFn: async () => {
      await apiClient.delete(`/events/${eventId}`)
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik silindi.', severity: 'success' })
      navigate('/events')
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik silinemedi.'), severity: 'error' }),
  })

  const { paginationModel, setPaginationModel, query: participantsQuery } = usePagedQuery({
    queryKey: ['event-participants', eventId],
    enabled: canManage,
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<EventParticipantListItemDto>>(`/events/${eventId}/participants`, { params: { pageIndex, pageSize } })).data,
  })

  const openEditDialog = () => {
    if (!eventQuery.data) return
    reset({
      title: eventQuery.data.title,
      description: eventQuery.data.description ?? '',
      location: eventQuery.data.location ?? '',
      startDateTime: toLocalInput(eventQuery.data.startDateUtc),
      endDateTime: toLocalInput(eventQuery.data.endDateUtc),
      capacity: eventQuery.data.capacity?.toString() ?? '',
    })
    editDialog.openDialog()
  }

  const participantColumns: GridColDef<EventParticipantListItemDto>[] = [
    { field: 'studentNumber', headerName: 'Öğrenci No', flex: 1, minWidth: 160 },
    {
      field: 'registeredAtUtc',
      headerName: 'Kayıt Tarihi',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
  ]

  // §23.2: `return null` hem yüklenirken hem 404'te bomboş ekran veriyordu — ikisi ayrıldı.
  if (eventQuery.isLoading) {
    return (
      <Stack spacing={2}>
        <Skeleton variant="text" width={280} height={40} />
        <Skeleton variant="rounded" height={180} />
        <Skeleton variant="rounded" height={240} />
      </Stack>
    )
  }

  const event = eventQuery.data
  if (!event) {
    return (
      <Alert severity="error" action={<Button size="small" onClick={() => navigate('/events')}>Etkinliklere Dön</Button>}>
        {extractErrorMessage(eventQuery.error, 'Etkinlik bulunamadı ya da görüntüleme yetkiniz yok.')}
      </Alert>
    )
  }

  const canEdit = canManage && (event.status === 'Draft' || event.status === 'Rejected')
  const canDelete = canManage && event.status === 'Draft'
  const canSubmit = canManage && event.status === 'Draft'
  // A-49: yayınlanmış etkinlik silinmez, iptal edilir.
  const canCancel = canManage && event.status === 'Published'

  return (
    <>
      <PageHeader
        title={event.title}
        description={event.clubName}
        action={
          <Stack direction="row" spacing={1}>
            {canUploadPoster && (
              <Button variant="outlined" disabled={posterMutation.isPending} onClick={() => posterInputRef.current?.click()}>
                Afiş Yükle
              </Button>
            )}
            {canSubmit && (
              <Button variant="outlined" disabled={submitMutation.isPending} onClick={() => submitMutation.mutate()}>
                Onaya Gönder
              </Button>
            )}
            {canEdit && (
              <Button variant="outlined" onClick={openEditDialog}>
                Düzenle
              </Button>
            )}
            {canCancel && (
              <Button variant="outlined" color="error" onClick={cancelDialog.openDialog}>
                Etkinliği İptal Et
              </Button>
            )}
            {canDelete && (
              <Button variant="outlined" color="error" onClick={() => setDeleteConfirmOpen(true)}>
                Sil
              </Button>
            )}
          </Stack>
        }
      />
      <input ref={posterInputRef} type="file" accept="image/*" hidden onChange={handlePosterFileChange} />

      <SectionCard sx={{ mb: 3 }}>
        <Stack spacing={1.5}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <EventStatusChip status={event.status} />
            <Typography variant="caption" color="text.secondary">
              {new Date(event.startDateUtc).toLocaleString('tr-TR')} — {new Date(event.endDateUtc).toLocaleString('tr-TR')}
            </Typography>
          </Stack>
          {/* A-49: iptal gerekçesi kalıcı hata değil ama kalıcı bir durum — snackbar değil sayfa içi Alert. */}
          {event.status === 'Cancelled' && (
            <Alert severity="error">
              Bu etkinlik iptal edildi.{event.cancellationReason ? ` Gerekçe: ${event.cancellationReason}` : ''}
            </Alert>
          )}

          {event.location && <Typography variant="body2">Yer: {event.location}</Typography>}
          <Typography variant="body2" color="text.secondary">
            {event.capacity ? `Kontenjan: ${event.capacity}` : 'Kontenjan sınırsız'}
          </Typography>
          <Typography variant="body2">{event.description || 'Açıklama eklenmemiş.'}</Typography>

          {event.status === 'Published' && (
            <Stack direction="row">
              {isRegistered ? (
                <Button variant="outlined" color="error" disabled={cancelMutation.isPending} onClick={() => cancelMutation.mutate()}>
                  Kaydımı İptal Et
                </Button>
              ) : (
                <Button variant="contained" disabled={registerMutation.isPending} onClick={() => registerMutation.mutate()}>
                  Katıl
                </Button>
              )}
            </Stack>
          )}
        </Stack>
      </SectionCard>

      {canManage && (
        <SectionCard title="Katılımcılar">
          <DataTable
            mobileHiddenFields={['registeredAtUtc']}
            rows={participantsQuery.data?.items ?? []}
            columns={participantColumns}
            getRowId={(row) => row.studentId}
            loading={participantsQuery.isFetching}
            paginationMode="server"
            rowCount={participantsQuery.data?.totalCount ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            pageSizeOptions={[10, 20, 50]}
            emptyTitle="Henüz katılımcı yok"
          />
        </SectionCard>
      )}

      <Dialog open={editDialog.open} onClose={editDialog.closeDialog} fullWidth maxWidth="xs">
        <DialogTitle>Etkinliği Düzenle</DialogTitle>
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
        </DialogContent>
        <DialogActions>
          <Button onClick={editDialog.closeDialog}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={isFormSubmitting || updateMutation.isPending}
            onClick={handleSubmit((values) => updateMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteConfirmOpen}
        title="Etkinliği sil"
        description="Bu taslak etkinliği silmek istediğinize emin misiniz?"
        confirmLabel="Sil"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteConfirmOpen(false)}
      />

      <Dialog
        open={cancelDialog.open}
        onClose={() => {
          cancelDialog.closeDialog()
          cancelForm.reset({ cancellationReason: '' })
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Etkinliği İptal Et</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            Etkinlik silinmez, iptal edildi olarak işaretlenir. Kayıtlı katılımcılara gerekçenizle birlikte e-posta gönderilir.
          </Typography>
          <Controller
            name="cancellationReason"
            control={cancelForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                autoFocus
                fullWidth
                multiline
                minRows={2}
                margin="dense"
                label="İptal gerekçesi"
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              cancelDialog.closeDialog()
              cancelForm.reset({ cancellationReason: '' })
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={cancelForm.formState.isSubmitting || cancelEventMutation.isPending}
            onClick={cancelForm.handleSubmit((values) => cancelEventMutation.mutate(values))}
          >
            Etkinliği İptal Et
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

function toLocalInput(isoDate: string): string {
  const date = new Date(isoDate)
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}
