import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { useState } from 'react'
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
import { announcementFormSchema, type AnnouncementFormValues } from '../schemas/announcementForm'
import type { AnnouncementListItemDto, PagedResult } from '../api/types'

const emptyAnnouncementFormValues: AnnouncementFormValues = { title: '', content: '', visibility: 'Members' }

export function ClubAnnouncementsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.AnnouncementsWrite)

  const createDialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<AnnouncementListItemDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AnnouncementListItemDto | null>(null)

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
      await apiClient.post(`/clubs/${clubId}/announcements`, values)
    },
    onSuccess: () => {
      notify({ message: 'Duyuru yayınlandı.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset(emptyAnnouncementFormValues)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru oluşturulamadı.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      if (!editTarget) return
      await apiClient.put(`/announcements/${editTarget.id}`, values)
    },
    onSuccess: () => {
      notify({ message: 'Duyuru güncellendi.', severity: 'success' })
      setEditTarget(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (announcement: AnnouncementListItemDto) => {
    editForm.reset({ title: announcement.title, content: announcement.content, visibility: announcement.visibility })
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
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Duyuru</DialogTitle>
        <DialogContent>
          <AnnouncementFormFields control={createForm.control} />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              createForm.reset(emptyAnnouncementFormValues)
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

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Duyuruyu Düzenle</DialogTitle>
        <DialogContent>
          <AnnouncementFormFields control={editForm.control} />
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

function AnnouncementFormFields({ control }: { control: Control<AnnouncementFormValues> }) {
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
        name="content"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} fullWidth multiline minRows={3} margin="dense" label="İçerik" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
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
