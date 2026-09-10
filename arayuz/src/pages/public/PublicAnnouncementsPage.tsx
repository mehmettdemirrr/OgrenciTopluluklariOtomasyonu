import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { Box, Grid, Typography, alpha } from '@mui/material'
import { apiClient } from '../../api/client'
import type { PagedResult, PublicAnnouncementListItemDto } from '../../api/types'
import { PublicAnnouncementGridCard } from '../../components/announcements/PublicAnnouncementGridCard'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { ResultPagination } from '../../components/ui/ResultPagination'
import { SearchField } from '../../components/ui/SearchField'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useSearchPagedQuery } from '../../hooks/useSearchPagedQuery'
import { useLocale } from '../../i18n/LocaleContext'

export function PublicAnnouncementsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('public.announcementsTitle'))

  const { search, setSearch, items, pageIndex, setPageIndex, pageCount, totalCount, query } =
    useSearchPagedQuery<PublicAnnouncementListItemDto>({
      queryKey: ['public-announcements'],
      queryFn: async ({ pageIndex: page, pageSize, search: term }) =>
        (
          await apiClient.get<PagedResult<PublicAnnouncementListItemDto>>('/public/announcements', {
            params: { pageIndex: page, pageSize, search: term || undefined },
          })
        ).data,
    })

  return (
    <>
      <Box
        sx={{
          mx: { xs: -2, sm: -3 },
          mt: { xs: -3, md: -5 },
          mb: 4,
          py: { xs: 6, md: 8 },
          textAlign: 'center',
          color: 'common.white',
          background: (theme) =>
            `linear-gradient(135deg, ${theme.palette.secondary.main} 0%, ${alpha(theme.palette.primary.dark, 0.88)} 100%)`,
        }}
      >
        <Typography variant="h3" component="h1" sx={{ fontWeight: 800, mb: 1, fontSize: { xs: 32, md: 48 } }}>
          {t('public.announcementsTitle')}
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.82, maxWidth: 560, mx: 'auto', px: 2 }}>
          {t('public.announcementsLead')}
        </Typography>
      </Box>

      <Box
        sx={{
          mb: 4,
          p: 3,
          borderRadius: 2.5,
          bgcolor: 'background.paper',
          border: '1px solid',
          borderColor: (theme) => alpha(theme.palette.secondary.main, 0.08),
          boxShadow: (theme) => `0 10px 30px ${alpha(theme.palette.secondary.main, 0.08)}`,
        }}
      >
        <SearchField value={search} onChange={setSearch} placeholder={t('common.searchAnnouncements')} />
      </Box>

      {query.isLoading ? (
        <CardGridSkeleton withMedia />
      ) : items.length === 0 ? (
        <EmptyState icon={CampaignOutlinedIcon} title={t('home.noAnnouncements')} />
      ) : (
        <Grid container spacing={3.5}>
          {items.map((announcement) => (
            <Grid key={announcement.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <PublicAnnouncementGridCard announcement={announcement} />
            </Grid>
          ))}
        </Grid>
      )}

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}
