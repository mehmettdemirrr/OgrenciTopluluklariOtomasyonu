import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Stack } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { EmptyState } from '../components/ui/EmptyState'
import { AnnouncementCard } from '../components/ui/AnnouncementCard'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import type { AnnouncementListItemDto, PagedResult } from '../api/types'

export function ClubAnnouncementsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.AnnouncementsWrite)

  const [deleteTarget, setDeleteTarget] = useState<AnnouncementListItemDto | null>(null)

  const { query: announcementsQuery } = usePagedQuery({
    queryKey: ['club-announcements', clubId],
    pageSize: 50,
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AnnouncementListItemDto>>(`/clubs/${clubId}/announcements`, { params: { pageIndex, pageSize } })).data,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['club-announcements', clubId] })

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
          <Button variant="contained" component={RouterLink} to={`/announcements/new?clubId=${clubId}&returnTo=/clubs/${clubId}`}>
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
                    <Button size="small" component={RouterLink} to={`/announcements/${announcement.id}/edit?clubId=${clubId}&returnTo=/clubs/${clubId}`}>
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
