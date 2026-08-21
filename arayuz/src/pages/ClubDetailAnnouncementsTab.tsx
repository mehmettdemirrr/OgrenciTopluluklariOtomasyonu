import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { EmptyState } from '../components/ui/EmptyState'
import { SectionCard } from '../components/ui/SectionCard'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import type { AnnouncementListItemDto, AnnouncementVisibility, PagedResult } from '../api/types'

export function ClubAnnouncementsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.AnnouncementsWrite)

  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [visibility, setVisibility] = useState<AnnouncementVisibility>('Members')
  const [deleteTarget, setDeleteTarget] = useState<AnnouncementListItemDto | null>(null)

  const { query: announcementsQuery } = usePagedQuery({
    queryKey: ['club-announcements', clubId],
    pageSize: 50,
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AnnouncementListItemDto>>(`/clubs/${clubId}/announcements`, { params: { pageIndex, pageSize } })).data,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['club-announcements', clubId] })

  const createMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post(`/clubs/${clubId}/announcements`, { title, content, visibility })
    },
    onSuccess: () => {
      notify({ message: 'Duyuru yayınlandı.', severity: 'success' })
      setCreateDialogOpen(false)
      setTitle('')
      setContent('')
      setVisibility('Members')
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru oluşturulamadı.'), severity: 'error' }),
  })

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
          <Button variant="contained" onClick={() => setCreateDialogOpen(true)}>
            Duyuru Oluştur
          </Button>
        </Stack>
      )}

      {!announcementsQuery.isLoading && items.length === 0 ? (
        <EmptyState icon={CampaignOutlinedIcon} title="Bu toplulukta duyuru yok" />
      ) : (
        <Stack spacing={2}>
          {items.map((announcement) => (
            <SectionCard
              key={announcement.id}
              action={
                canWrite && (
                  <Button size="small" color="error" onClick={() => setDeleteTarget(announcement)}>
                    Kaldır
                  </Button>
                )
              }
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {announcement.title}
                </Typography>
                <AnnouncementVisibilityChip visibility={announcement.visibility} />
              </Stack>
              <Typography variant="caption" color="text.secondary">
                {new Date(announcement.publishedAtUtc).toLocaleString('tr-TR')}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1 }}>
                {announcement.content}
              </Typography>
            </SectionCard>
          ))}
        </Stack>
      )}

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Duyuru</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Başlık" value={title} onChange={(e) => setTitle(e.target.value)} />
          <TextField fullWidth multiline minRows={3} margin="dense" label="İçerik" value={content} onChange={(e) => setContent(e.target.value)} />
          <TextField select fullWidth margin="dense" label="Görünürlük" value={visibility} onChange={(e) => setVisibility(e.target.value as AnnouncementVisibility)}>
            <MenuItem value="Members">Yalnızca Üyeler</MenuItem>
            <MenuItem value="Public">Herkese Açık</MenuItem>
          </TextField>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={title.trim() === '' || content.trim() === '' || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            Yayınla
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
