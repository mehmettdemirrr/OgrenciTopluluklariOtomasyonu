import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, FormControl, FormControlLabel, FormLabel, Radio, RadioGroup, Skeleton, TextField } from '@mui/material'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { useNotifier } from '../../notifications/NotifierProvider'
import { FormPageShell } from '../../components/ui/FormPageShell'
import { RemoteSelect } from '../../components/ui/RemoteSelect'
import { RichTextEditor } from '../../components/richtext/RichTextEditor'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useReturnTo } from '../../hooks/useReturnTo'
import { useLocale } from '../../i18n/LocaleContext'
import { emptyEventFormValues, eventFormSchema, plainTextToDoc, toEventPayload, toLocalInputValue, type EventFormValues } from '../../schemas/eventForm'
import type { ClubListItemDto, EventListItemDto, PagedResult } from '../../api/types'

export function EventFormPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const notify = useNotifier()
  const queryClient = useQueryClient()

  const eventId = id ? Number(id) : null
  const isEdit = eventId !== null
  const clubIdParam = searchParams.get('clubId')
  const presetClubId = clubIdParam ? Number(clubIdParam) : null
  const { returnTo, goBack } = useReturnTo(isEdit ? `/events/${eventId}` : '/events')

  const {
    control,
    handleSubmit,
    reset,
    formState: { isDirty, isSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventFormSchema),
    defaultValues: emptyEventFormValues,
  })

  // Düzenleme kipinde mevcut kaydı forma yükle.
  const eventQuery = useQuery({
    queryKey: ['events', eventId],
    queryFn: async () => (await apiClient.get<EventListItemDto>(`/events/${eventId}`)).data,
    enabled: isEdit,
  })

  useDocumentTitle(isEdit ? eventQuery.data?.title : t('form.createEvent'))

  useEffect(() => {
    if (isEdit) {
      if (!eventQuery.data) {
        return
      }
      const event = eventQuery.data
      reset({
        clubId: event.clubId,
        title: event.title,
        // A-71: eski düz metin kayıt düzenlemeye açılınca kaybolmasın diye tek paragraflık belgeye sarılır.
        descriptionJson: event.descriptionJson ?? (event.description ? plainTextToDoc(event.description) : ''),
        location: event.location ?? '',
        startDateTime: toLocalInputValue(event.startDateUtc),
        endDateTime: toLocalInputValue(event.endDateUtc),
        capacity: event.capacity ? String(event.capacity) : '',
        audience: event.audience,
      })
      return
    }

    if (presetClubId !== null) {
      reset({ ...emptyEventFormValues, clubId: presetClubId })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEdit, eventQuery.data, presetClubId, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: EventFormValues) => {
      const payload = toEventPayload(values)
      if (isEdit) {
        await apiClient.put(`/events/${eventId}`, payload)
        return eventId
      }
      const clubId = presetClubId ?? values.clubId
      const response = await apiClient.post<number>(`/clubs/${clubId}/events`, payload)
      return response.data
    },
    onSuccess: (savedId) => {
      notify({ message: isEdit ? 'Etkinlik güncellendi.' : 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events'] })
      queryClient.invalidateQueries({ queryKey: ['club-events'] })
      queryClient.invalidateQueries({ queryKey: ['events', savedId] })
      goBack()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik kaydedilemedi.'), severity: 'error' }),
  })

  if (isEdit && eventQuery.isLoading) {
    return <Skeleton variant="rounded" height={420} />
  }

  return (
    <FormPageShell
      title={isEdit ? 'Etkinliği Düzenle' : t('form.createEvent')}
      backTo={returnTo}
      isDirty={isDirty}
      isSubmitting={isSubmitting || saveMutation.isPending}
      submitLabel={isEdit ? t('form.saveEvent') : t('form.createEvent')}
      onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
      onCancel={goBack}
    >
      {!isEdit && presetClubId === null && (
        <Controller
          name="clubId"
          control={control}
          render={({ field, fieldState }) => (
            <RemoteSelect<ClubListItemDto>
              label={t('form.selectClub')}
              value={field.value || null}
              onChange={(value) => field.onChange(value ?? 0)}
              queryKey={['clubs', 'event-form']}
              fetchOptions={async (term) =>
                (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
                  params: { pageIndex: 0, pageSize: 20, search: term || undefined, isActive: true },
                })).data.items
              }
              getOptionId={(club) => club.id}
              getOptionLabel={(club) => club.name}
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      )}

      <Controller
        name="title"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="normal" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="descriptionJson"
        control={control}
        render={({ field }) => (
          <Box sx={{ mt: 2, mb: 2 }}>
            <RichTextEditor value={field.value || null} onChange={field.onChange} />
          </Box>
        )}
      />
      <Controller name="location" control={control} render={({ field }) => <TextField {...field} fullWidth margin="normal" label="Yer" />} />
      <Controller
        name="startDateTime"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            fullWidth
            margin="normal"
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
            margin="normal"
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
            margin="normal"
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
          <FormControl margin="normal">
            <FormLabel>Kimler katılabilir?</FormLabel>
            <RadioGroup {...field} row>
              <FormControlLabel value="Public" control={<Radio />} label="Herkese açık" />
              <FormControlLabel value="ClubMembers" control={<Radio />} label="Sadece topluluk üyeleri" />
            </RadioGroup>
          </FormControl>
        )}
      />
    </FormPageShell>
  )
}
