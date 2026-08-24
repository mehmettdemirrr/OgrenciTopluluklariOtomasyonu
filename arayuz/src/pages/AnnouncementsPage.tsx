import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { useState } from 'react'
import { Controller, useForm, type Control } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SearchField } from '../components/ui/SearchField'
import { SectionCard } from '../components/ui/SectionCard'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import { announcementFormSchema, type AnnouncementFormValues } from '../schemas/announcementForm'
import type { AnnouncementListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

const emptyAnnouncementFormValues: AnnouncementFormValues = { title: '', content: '', visibility: 'Public' }

export function AnnouncementsPage() {
  useDocumentTitle('Duyurular')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canCreateGlobal = hasPermission(Permissions.AnnouncementsGlobal)

  const createDialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<AnnouncementListItemDto | null>(null)

  const createForm = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })
  const editForm = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })

  // A-50: başlık araması sunucuda (LIKE), debounce'lu.
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)

  const { paginationModel, setPaginationModel, query: feedQuery } = usePagedQuery({
    queryKey: ['announcements-feed', debouncedSearch],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AnnouncementListItemDto>>('/announcements', {
        params: { pageIndex, pageSize, search: debouncedSearch || undefined },
      })).data,
  })

  const createGlobalMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      await apiClient.post('/announcements', values)
    },
    onSuccess: () => {
      notify({ message: 'Duyuru yayınlandı.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset(emptyAnnouncementFormValues)
      queryClient.invalidateQueries({ queryKey: ['announcements-feed'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru oluşturulamadı.'), severity: 'error' }),
  })

  const updateGlobalMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      if (!editTarget) return
      await apiClient.put(`/announcements/${editTarget.id}`, values)
    },
    onSuccess: () => {
      notify({ message: 'Duyuru güncellendi.', severity: 'success' })
      setEditTarget(null)
      queryClient.invalidateQueries({ queryKey: ['announcements-feed'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (announcement: AnnouncementListItemDto) => {
    editForm.reset({ title: announcement.title, content: announcement.content, visibility: announcement.visibility })
    setEditTarget(announcement)
  }

  const items = feedQuery.data?.items ?? []

  return (
    <>
      <PageHeader
        title="Duyurular"
        description="Topluluk ve sistem duyurularının akışı."
        action={
          canCreateGlobal && (
            <Button variant="contained" onClick={createDialog.openDialog}>
              Sistem Duyurusu Oluştur
            </Button>
          )
        }
      />

      <Box sx={{ mb: 3 }}>
        <SearchField
          value={search}
          onChange={(value) => {
            setPaginationModel({ ...paginationModel, page: 0 })
            setSearch(value)
          }}
          placeholder="Duyuru ara…"
        />
      </Box>

      {!feedQuery.isLoading && items.length === 0 ? (
        <EmptyState icon={CampaignOutlinedIcon} title="Henüz duyuru yok" description="Yeni bir duyuru yayınlandığında burada görünecek." />
      ) : (
        <Stack spacing={2}>
          {items.map((announcement) => (
            <SectionCard
              key={announcement.id}
              action={
                canCreateGlobal &&
                announcement.clubId === null && (
                  <Button size="small" onClick={() => openEditDialog(announcement)}>
                    Düzenle
                  </Button>
                )
              }
            >
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

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          createForm.reset(emptyAnnouncementFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Sistem Duyurusu</DialogTitle>
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
            disabled={createForm.formState.isSubmitting || createGlobalMutation.isPending}
            onClick={createForm.handleSubmit((values) => createGlobalMutation.mutate(values))}
          >
            Yayınla
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Sistem Duyurusunu Düzenle</DialogTitle>
        <DialogContent>
          <AnnouncementFormFields control={editForm.control} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editForm.formState.isSubmitting || updateGlobalMutation.isPending}
            onClick={editForm.handleSubmit((values) => updateGlobalMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
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
            <MenuItem value="Public">Herkese Açık</MenuItem>
            <MenuItem value="Members">Yalnızca Üyeler</MenuItem>
          </TextField>
        )}
      />
    </>
  )
}
