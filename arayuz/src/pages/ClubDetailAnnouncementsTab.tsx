import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { type ChangeEvent, useState } from 'react'
import { Controller, useForm, type Control } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { EmptyState } from '../components/ui/EmptyState'
import { AnnouncementCard } from '../components/ui/AnnouncementCard'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import { RichTextEditor } from '../components/richtext/RichTextEditor'
import { announcementFormSchema, type AnnouncementFormValues } from '../schemas/announcementForm'
import type { AnnouncementListItemDto, PagedResult } from '../api/types'

const emptyAnnouncementFormValues: AnnouncementFormValues = { title: '', contentJson: '', visibility: 'Members' }

async function uploadAnnouncementImage(announcementId: number, image: File) {
  const formData = new FormData()
  formData.append('file', image)
  await apiClient.post(`/announcements/${announcementId}/image`, formData)
}

// docs/MIMARI.md · A-71: eski düz metin duyuru düzenlemeye açılınca kaybolmasın diye
// editöre tek paragraflık bir belge olarak yüklenir.
function plainTextToDoc(text: string): string {
  return JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: text ? [{ type: 'text', text }] : [] }],
  })
}

export function ClubAnnouncementsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.AnnouncementsWrite)

  const createDialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<AnnouncementListItemDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AnnouncementListItemDto | null>(null)
  const [createImage, setCreateImage] = useState<File | null>(null)
  const [editImage, setEditImage] = useState<File | null>(null)

  const createForm = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })
  const editForm = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })

  const { query: announcementsQuery } = usePagedQuery({
    queryKey: ['club-announcements', clubId],
    pageSize: 50,
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AnnouncementListItemDto>>(`/clubs/${clubId}/announcements`, { params: { pageIndex, pageSize } })).data,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['club-announcements', clubId] })

  const createMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      const response = await apiClient.post<number>(`/clubs/${clubId}/announcements`, values)
      if (createImage) {
        await uploadAnnouncementImage(response.data, createImage)
      }
    },
    onSuccess: () => {
      notify({ message: 'Duyuru yayınlandı.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset(emptyAnnouncementFormValues)
      setCreateImage(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru oluşturulamadı.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      if (!editTarget) return
      await apiClient.put(`/announcements/${editTarget.id}`, values)
      if (editImage) {
        await uploadAnnouncementImage(editTarget.id, editImage)
      }
    },
    onSuccess: () => {
      notify({ message: 'Duyuru güncellendi.', severity: 'success' })
      setEditTarget(null)
      setEditImage(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (announcement: AnnouncementListItemDto) => {
    editForm.reset({
      title: announcement.title,
      contentJson: announcement.contentJson ?? plainTextToDoc(announcement.content),
      visibility: announcement.visibility,
    })
    setEditImage(null)
    setEditTarget(announcement)
  }

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!deleteTarget) return
      await apiClient.delete(`/announcements/${deleteTarget.id}`)
    },
    onSuccess: () => {
      notify({ message: 'Duyuru kaldırıldı.', severity: 'success' })
      setDeleteTarget(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru kaldırılamadı.'), severity: 'error' }),
  })

  const items = announcementsQuery.data?.items ?? []

  return (
    <>
      {canWrite && (
        <Stack direction="row" sx={{ mb: 2 }}>
          <Button variant="contained" onClick={createDialog.openDialog}>
            Duyuru Oluştur
          </Button>
        </Stack>
      )}

      {!announcementsQuery.isLoading && items.length === 0 ? (
        <EmptyState icon={CampaignOutlinedIcon} title="Bu toplulukta duyuru yok" />
      ) : (
        <Stack spacing={1.5}>
          {items.map((announcement) => (
            <AnnouncementCard
              key={announcement.id}
              title={announcement.title}
              content={announcement.content}
              contentJson={announcement.contentJson}
              imageFileId={announcement.imageFileId}
              publishedAtUtc={announcement.publishedAtUtc}
              meta={new Date(announcement.publishedAtUtc).toLocaleString('tr-TR')}
              chip={<AnnouncementVisibilityChip visibility={announcement.visibility} />}
              action={
                canWrite ? (
                  <Stack direction="row" spacing={1}>
                    <Button size="small" onClick={() => openEditDialog(announcement)}>
                      Düzenle
                    </Button>
                    <Button size="small" color="error" onClick={() => setDeleteTarget(announcement)}>
                      Kaldır
                    </Button>
                  </Stack>
                ) : undefined
              }
            />
          ))}
        </Stack>
      )}

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          createForm.reset(emptyAnnouncementFormValues)
          setCreateImage(null)
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Yeni Duyuru</DialogTitle>
        <DialogContent>
          <AnnouncementFormFields control={createForm.control} image={createImage} onImageChange={setCreateImage} />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              createForm.reset(emptyAnnouncementFormValues)
              setCreateImage(null)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createForm.formState.isSubmitting || createMutation.isPending}
            onClick={createForm.handleSubmit((values) => createMutation.mutate(values))}
          >
            Yayınla
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="sm">
        <DialogTitle>Duyuruyu Düzenle</DialogTitle>
        <DialogContent>
          <AnnouncementFormFields control={editForm.control} image={editImage} onImageChange={setEditImage} existingImageFileId={editTarget?.imageFileId} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editForm.formState.isSubmitting || updateMutation.isPending}
            onClick={editForm.handleSubmit((values) => updateMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Duyuruyu kaldır"
        description={deleteTarget ? `"${deleteTarget.title}" duyurusunu kaldırmak istediğinize emin misiniz?` : undefined}
        confirmLabel="Kaldır"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  )
}

function AnnouncementFormFields({
  control,
  image,
  onImageChange,
  existingImageFileId,
}: {
  control: Control<AnnouncementFormValues>
  image: File | null
  onImageChange: (file: File | null) => void
  existingImageFileId?: number | null
}) {
  const handleImageChange = (event: ChangeEvent<HTMLInputElement>) => {
    onImageChange(event.target.files?.[0] ?? null)
    event.target.value = ''
  }

  return (
    <>
      <Controller
        name="title"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="dense" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="contentJson"
        control={control}
        render={({ field, fieldState }) => (
          <Box sx={{ mt: 1, mb: 0.5 }}>
            <RichTextEditor value={field.value || null} onChange={field.onChange} />
            {fieldState.error && (
              <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
                {fieldState.error.message}
              </Typography>
            )}
          </Box>
        )}
      />
      <Box sx={{ mt: 1.5 }}>
        <Button component="label" size="small" variant="outlined">
          {image ? image.name : existingImageFileId ? 'Kapak görselini değiştir' : 'Kapak görseli ekle'}
          <input type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={handleImageChange} />
        </Button>
        {(image || existingImageFileId) && (
          <Box
            component="img"
            src={image ? URL.createObjectURL(image) : `/api/files/${existingImageFileId}`}
            alt=""
            sx={{ display: 'block', mt: 1, height: 80, borderRadius: 1.5, objectFit: 'cover' }}
          />
        )}
      </Box>
      <Controller
        name="visibility"
        control={control}
        render={({ field }) => (
          <TextField {...field} select fullWidth margin="dense" label="Görünürlük">
            <MenuItem value="Members">Yalnızca Üyeler</MenuItem>
            <MenuItem value="Public">Herkese Açık</MenuItem>
          </TextField>
        )}
      />
    </>
  )
}
