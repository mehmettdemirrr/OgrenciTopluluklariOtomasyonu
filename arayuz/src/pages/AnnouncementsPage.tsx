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
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import type { AnnouncementListItemDto, AnnouncementVisibility, PagedResult } from '../api/types'

export function AnnouncementsPage() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canCreateGlobal = hasPermission(Permissions.AnnouncementsGlobal)

  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [visibility, setVisibility] = useState<AnnouncementVisibility>('Public')

  const { paginationModel, setPaginationModel, query: feedQuery } = usePagedQuery({
    queryKey: ['announcements-feed'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AnnouncementListItemDto>>('/announcements', { params: { pageIndex, pageSize } })).data,
  })

  const createGlobalMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/announcements', { title, content, visibility })
    },
    onSuccess: () => {
      notify({ message: 'Duyuru yayınlandı.', severity: 'success' })
      setCreateDialogOpen(false)
      setTitle('')
      setContent('')
      setVisibility('Public')
      queryClient.invalidateQueries({ queryKey: ['announcements-feed'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru oluşturulamadı.'), severity: 'error' }),
  })

  const items = feedQuery.data?.items ?? []

  return (
    <>
      <PageHeader
        title="Duyurular"
        description="Topluluk ve sistem duyurularının akışı."
        action={
          canCreateGlobal && (
            <Button variant="contained" onClick={() => setCreateDialogOpen(true)}>
              Sistem Duyurusu Oluştur
            </Button>
          )
        }
      />

      {!feedQuery.isLoading && items.length === 0 ? (
        <EmptyState icon={CampaignOutlinedIcon} title="Henüz duyuru yok" description="Yeni bir duyuru yayınlandığında burada görünecek." />
      ) : (
        <Stack spacing={2}>
          {items.map((announcement) => (
            <SectionCard key={announcement.id}>
              <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {announcement.title}
                </Typography>
                <AnnouncementVisibilityChip visibility={announcement.visibility} />
              </Stack>
              <Typography variant="caption" color="text.secondary">
                {announcement.clubName ?? 'Sistem Duyurusu'} · {new Date(announcement.publishedAtUtc).toLocaleString('tr-TR')}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1 }}>
                {announcement.content}
              </Typography>
            </SectionCard>
          ))}
        </Stack>
      )}

      {feedQuery.data && feedQuery.data.totalCount > paginationModel.pageSize && (
        <Stack direction="row" spacing={1} sx={{ mt: 2, justifyContent: 'center' }}>
          <Button
            size="small"
            disabled={paginationModel.page === 0}
            onClick={() => setPaginationModel({ ...paginationModel, page: paginationModel.page - 1 })}
          >
            Önceki
          </Button>
          <Button
            size="small"
            disabled={(paginationModel.page + 1) * paginationModel.pageSize >= feedQuery.data.totalCount}
            onClick={() => setPaginationModel({ ...paginationModel, page: paginationModel.page + 1 })}
          >
            Sonraki
          </Button>
        </Stack>
      )}

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Sistem Duyurusu</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Başlık" value={title} onChange={(e) => setTitle(e.target.value)} />
          <TextField fullWidth multiline minRows={3} margin="dense" label="İçerik" value={content} onChange={(e) => setContent(e.target.value)} />
          <TextField select fullWidth margin="dense" label="Görünürlük" value={visibility} onChange={(e) => setVisibility(e.target.value as AnnouncementVisibility)}>
            <MenuItem value="Public">Herkese Açık</MenuItem>
            <MenuItem value="Members">Yalnızca Üyeler</MenuItem>
          </TextField>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={title.trim() === '' || content.trim() === '' || createGlobalMutation.isPending}
            onClick={() => createGlobalMutation.mutate()}
          >
            Yayınla
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
