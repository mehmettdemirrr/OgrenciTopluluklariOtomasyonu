import { Box, Button, Grid, Stack } from '@mui/material'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { AnnouncementGridCard } from '../components/announcements/AnnouncementGridCard'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SearchField } from '../components/ui/SearchField'
import { AnnouncementVisibilityChip } from '../components/ui/StatusChip'
import type { AnnouncementListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

export function AnnouncementsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('pages.announcements'))

  const { hasPermission } = useAuth()
  const canCreateGlobal = hasPermission(Permissions.AnnouncementsGlobal)

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

  const items = feedQuery.data?.items ?? []

  return (
    <>
      <PageHeader
        title={t('pages.announcements')}
        description={t('pages.announcementsLead')}
        action={
          canCreateGlobal && (
            <Button variant="contained" component={RouterLink} to="/announcements/new?returnTo=/announcements">
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
        <Grid container spacing={3.5}>
          {items.map((announcement) => (
            <Grid key={announcement.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <AnnouncementGridCard
                title={announcement.title}
                content={announcement.content}
                clubName={announcement.clubName ?? 'Sistem Duyurusu'}
                publishedAtUtc={announcement.publishedAtUtc}
                imageFileId={announcement.imageFileId}
                badges={<AnnouncementVisibilityChip visibility={announcement.visibility} />}
                actions={
                  canCreateGlobal && announcement.clubId === null ? (
                    <Button size="small" component={RouterLink} to={`/announcements/${announcement.id}/edit?returnTo=/announcements`}>
                      Düzenle
                    </Button>
                  ) : undefined
                }
              />
            </Grid>
          ))}
        </Grid>
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
    </>
  )
}
