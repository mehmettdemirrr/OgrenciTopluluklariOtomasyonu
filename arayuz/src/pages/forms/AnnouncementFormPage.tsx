import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, MenuItem, Skeleton, TextField, Typography } from '@mui/material'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { useNotifier } from '../../notifications/NotifierProvider'
import { FormPageShell } from '../../components/ui/FormPageShell'
import { RichTextEditor } from '../../components/richtext/RichTextEditor'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useReturnTo } from '../../hooks/useReturnTo'
import { useLocale } from '../../i18n/LocaleContext'
import { announcementFormSchema, emptyAnnouncementFormValues, plainTextToDoc, type AnnouncementFormValues } from '../../schemas/announcementForm'
import type { AnnouncementListItemDto } from '../../api/types'

export function AnnouncementFormPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const notify = useNotifier()
  const queryClient = useQueryClient()
  const imageInputRef = useRef<HTMLInputElement>(null)
  const [pendingImage, setPendingImage] = useState<File | null>(null)

  const announcementId = id ? Number(id) : null
  const isEdit = announcementId !== null
  const clubIdParam = searchParams.get('clubId')
  const clubId = clubIdParam ? Number(clubIdParam) : null
  const { returnTo, goBack } = useReturnTo(clubId ? `/clubs/${clubId}` : '/announcements')

  const {
    control,
    handleSubmit,
    reset,
    formState: { isDirty, isSubmitting },
  } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })

  const announcementQuery = useQuery({
    queryKey: ['announcement', announcementId],
    queryFn: async () => (await apiClient.get<AnnouncementListItemDto>(`/announcements/${announcementId}`)).data,
    enabled: isEdit,
  })

  useDocumentTitle(isEdit ? announcementQuery.data?.title : t('form.createAnnouncement'))

  useEffect(() => {
    if (!isEdit || !announcementQuery.data) {
      return
    }
    const announcement = announcementQuery.data
    reset({
      title: announcement.title,
      contentJson: announcement.contentJson ?? plainTextToDoc(announcement.content),
      visibility: announcement.visibility,
    })
  }, [isEdit, announcementQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      let savedId = announcementId
      if (isEdit) {
        await apiClient.put(`/announcements/${announcementId}`, values)
      } else if (clubId) {
        savedId = (await apiClient.post<number>(`/clubs/${clubId}/announcements`, values)).data
      } else {
        savedId = (await apiClient.post<number>('/announcements', values)).data
      }

      // K-42: görsel, duyuru kaydedildikten SONRA kendi ucuna yüklenir.
      if (pendingImage && savedId) {
        const formData = new FormData()
        formData.append('file', pendingImage)
        await apiClient.post(`/announcements/${savedId}/image`, formData)
      }
      return savedId
    },
    onSuccess: () => {
      notify({ message: isEdit ? 'Duyuru güncellendi.' : 'Duyuru yayınlandı.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['announcements-feed'] })
      queryClient.invalidateQueries({ queryKey: ['club-announcements'] })
      goBack()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru kaydedilemedi.'), severity: 'error' }),
  })

  const handleImageChange = (event: ChangeEvent<HTMLInputElement>) => {
    setPendingImage(event.target.files?.[0] ?? null)
    event.target.value = ''
  }

  if (isEdit && announcementQuery.isLoading) {
    return <Skeleton variant="rounded" height={420} />
  }

  return (
    <FormPageShell
      title={isEdit ? 'Duyuruyu Düzenle' : clubId ? 'Yeni Duyuru' : 'Sistem Duyurusu'}
      backTo={returnTo}
      isDirty={isDirty || pendingImage !== null}
      isSubmitting={isSubmitting || saveMutation.isPending}
      submitLabel={isEdit ? t('form.saveAnnouncement') : t('form.createAnnouncement')}
      onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
      onCancel={goBack}
    >
      <Controller
        name="title"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="normal" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="contentJson"
        control={control}
        render={({ field, fieldState }) => (
          <Box sx={{ mt: 2, mb: 1 }}>
            <RichTextEditor value={field.value || null} onChange={field.onChange} />
            {fieldState.error && (
              <Typography variant="caption" color="error">
                {fieldState.error.message}
              </Typography>
            )}
          </Box>
        )}
      />
      <Controller
        name="visibility"
        control={control}
        render={({ field }) => (
          <TextField {...field} select fullWidth margin="normal" label="Görünürlük">
            <MenuItem value="Members">Üyelere özel</MenuItem>
            <MenuItem value="Public">Herkese açık</MenuItem>
          </TextField>
        )}
      />

      <input ref={imageInputRef} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={handleImageChange} />
      <Box sx={{ mt: 2 }}>
        <Button variant="outlined" startIcon={<ImageOutlinedIcon />} onClick={() => imageInputRef.current?.click()}>
          {pendingImage ? pendingImage.name : announcementQuery.data?.imageFileId ? 'Kapak görselini değiştir' : 'Kapak Görseli Seç'}
        </Button>
        {(pendingImage || announcementQuery.data?.imageFileId) && (
          <Box
            component="img"
            src={pendingImage ? URL.createObjectURL(pendingImage) : `/api/files/${announcementQuery.data?.imageFileId}`}
            alt=""
            sx={{ display: 'block', mt: 1, height: 80, borderRadius: 1.5, objectFit: 'cover' }}
          />
        )}
      </Box>
    </FormPageShell>
  )
}
