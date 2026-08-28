import { Box, Card, CardContent, CardMedia, Chip, Grid, Typography, alpha } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import { SearchField } from '../../components/ui/SearchField'
import { useSearchPagedQuery } from '../../hooks/useSearchPagedQuery'
import type { PagedResult, PublicClubListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'

export function PublicClubsPage() {
  useDocumentTitle('Kulüpler')

  // A-50/Y-62: arama sunucuda. Önceden 200 istenip 100 alınıyor ve gerisi istemcide ayıklanıyordu.
  const { search, setSearch, items, pageIndex, setPageIndex, pageCount, totalCount, query } =
    useSearchPagedQuery<PublicClubListItemDto>({
      queryKey: ['public-clubs'],
      queryFn: async ({ pageIndex: page, pageSize, search: term }) =>
        (await apiClient.get<PagedResult<PublicClubListItemDto>>('/public/clubs', {
          params: { pageIndex: page, pageSize, search: term || undefined },
        })).data,
    })

  return (
    <>
      <PageHeader title="Kulüpler" description="Kampüsteki aktif toplulukları keşfedin." backTo="/" />

      <Box sx={{ mb: 3 }}>
        <SearchField value={search} onChange={setSearch} placeholder="Kulüp ara…" />
      </Box>

      {query.isLoading && <CardGridSkeleton withMedia />}

      {!query.isLoading && items.length === 0 && (
        <EmptyState icon={GroupsOutlinedIcon} title="Kulüp bulunamadı" description="Arama kriterinizi değiştirmeyi deneyin." />
      )}

      <Grid container spacing={2.5}>
        {items.map((club) => (
          <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card
              variant="outlined"
              component={RouterLink}
              to={`/kulupler/${club.id}`}
              sx={{ display: 'flex', flexDirection: 'column', height: '100%', textDecoration: 'none', color: 'inherit' }}
            >
              {club.logoFileId ? (
                <CardMedia component="img" height={140} image={`/api/files/${club.logoFileId}`} alt="" sx={{ objectFit: 'contain', bgcolor: 'grey.50', p: 2 }} />
              ) : (
                <Box
                  sx={{
                    height: 140,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    background: (theme) =>
                      `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.12)} 100%)`,
                  }}
                >
                  <GroupsOutlinedIcon sx={{ fontSize: 48, color: 'primary.dark' }} />
                </Box>
              )}
              <CardContent sx={{ flex: 1 }}>
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 0.75 }} noWrap>
                  {club.name}
                </Typography>
                {/* A-42: anonim vitrinde kategori FİLTRESİ yok, yalnızca rozet — filtre için
                    /api/public/club-categories açmak gerekirdi ve anonim yüzey dar kalıyor.
                    Backend categoryId parametresini destekliyor; uç sonradan eklenebilir. */}
                {club.clubCategoryName && (
                  <Chip size="small" variant="outlined" color="primary" label={club.clubCategoryName} sx={{ mb: 1 }} />
                )}
                <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                  {club.description || 'Açıklama eklenmemiş.'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}
