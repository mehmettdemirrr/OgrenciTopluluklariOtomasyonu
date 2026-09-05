import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  Radio,
  RadioGroup,
  Stack,
  TextField,
} from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { EventAudienceChip, EventStatusChip } from '../components/ui/StatusChip'
import { RichTextEditor } from '../components/richtext/RichTextEditor'
import { emptyEventFormValues, eventFormSchema, toEventPayload, type EventFormValues } from '../schemas/eventForm'
import type { EventListItemDto, PagedResult } from '../api/types'

/**
 * docs/MIMARI.md · A-75/Y-81: `canManage` çağıranın BU kulüpteki `EventsManage` kapasitesidir
 * (ClubDetailPage'den gelir) — global `events.write` izni değil. Yetkisizken yönetim ucu
 * (`GET /api/clubs/{id}/events`) hiç çağrılmaz, yayınlanmış etkinlik ucu kullanılır (K-45).
 */
export function ClubEventsTab({ clubId, canManage }: { clubId: number; canManage: boolean }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()

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
    queryKey: ['club-events', clubId, canManage],
    queryFn: async (pageIndex, pageSize) =>
      canManage
        ? (await apiClient.get<PagedResult<EventListItemDto>>(`/clubs/${clubId}/events`, { params: { pageIndex, pageSize } })).data
        : (await apiClient.get<PagedResult<EventListItemDto>>('/events', { params: { clubId, pageIndex, pageSize } })).data,
  })

  const createEventMutation = useMutation({
    mutationFn: async (values: EventFormValues) => {
      await apiClient.post(`/clubs/${clubId}/events`, toEventPayload(values))
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      createDialog.closeDialog()
      reset(emptyEventFormValues)
      queryClient.invalidateQueries({ queryKey: ['club-events', clubId, canManage] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik oluşturulamadı.'), severity: 'error' }),
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
      width: 120,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" component={RouterLink} to={`/events/${params.row.id}`}>
          Detay
        </Button>
      ),
    },
  ]

  return (
    <>
      {canManage && (
        <Stack direction="row" sx={{ mb: 2 }}>
          <Button variant="contained" onClick={createDialog.openDialog}>
            Etkinlik Oluştur
          </Button>
        </Stack>
      )}

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
        emptyTitle={canManage ? 'Bu toplulukta etkinlik yok' : 'Bu toplulukta yayınlanmış etkinlik yok'}
      />

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          reset(emptyEventFormValues)
        }}
        fullWidth
        maxWidth="sm"
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
            name="descriptionJson"
            control={control}
            render={({ field }) => (
              <Box sx={{ mt: 1, mb: 1.5 }}>
                <RichTextEditor value={field.value || null} onChange={field.onChange} />
              </Box>
            )}
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
