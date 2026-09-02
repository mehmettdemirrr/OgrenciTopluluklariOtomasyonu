import { Alert, Box, Card, CardContent, CardMedia, Chip, Grid, Typography, alpha } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import type { PagedResult, PublicClubCategoryDto, PublicClubListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import { ClubBrowseFilters } from './ClubBrowseFilters'

const PAGE_SIZE = 12

export function PublicClubsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('public.clubsTitle'))
  const [categoryId, setCategoryId] = useState<number | null>(null)
  const [letter, setLetter] = useState<string | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [pageIndex, setPageIndex] = useState(0)

  const categoriesQuery = useQuery({
    queryKey: ['public-club-categories'],
    queryFn: async () => (await apiClient.get<PublicClubCategoryDto[]>('/public/club-categories')).data,
  })

  // Filtreler queryKey'den okunur — kapanış (stale closure) ve placeholder eski liste bırakmasın.
  const query = useQuery({
    queryKey: ['public-clubs', { pageIndex, pageSize: PAGE_SIZE, search: appliedSearch, categoryId, letter }],
    queryFn: async ({ queryKey }) => {
      const [, params] = queryKey as [string, { pageIndex: number; pageSize: number; search: string; categoryId: number | null; letter: string | null }]
      return (
        await apiClient.get<PagedResult<PublicClubListItemDto>>('/public/clubs', {
          params: {
            pageIndex: params.pageIndex,
            pageSize: params.pageSize,
            search: params.search || undefined,
            categoryId: params.categoryId ?? undefined,
            letter: params.letter ?? undefined,
          },
        })
      ).data
    },
  })

  const items = query.data?.items ?? []
  const totalCount = query.data?.totalCount ?? 0
  const pageCount = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  const applyCategory = (nextId: number | null) => {
    setCategoryId(nextId)
    setPageIndex(0)
  }

  const applyLetter = (next: string | null) => {
    setLetter(next)
    setPageIndex(0)
  }

  return (
    <>
      <PageHeader
        title={t('public.clubsTitle')}
        description={t('public.clubsLead', { university: t('brand.university') })}
        backTo="/"
      />

      <ClubBrowseFilters
        categories={categoriesQuery.data ?? []}
        categoryId={categoryId}
        onCategoryChange={applyCategory}
        searchInput={searchInput}
        onSearchInputChange={setSearchInput}
        onSearchSubmit={() => {
          setAppliedSearch(searchInput.trim())
          setPageIndex(0)
        }}
        letter={letter}
        onLetterChange={applyLetter}
      />

      {query.isError && <Alert severity="error" sx={{ mb: 2 }}>{extractErrorMessage(query.error, t('public.clubsEmpty'))}</Alert>}

      {query.isPending && <CardGridSkeleton withMedia />}

      {!query.isPending && items.length === 0 && (
        <EmptyState icon={GroupsOutlinedIcon} title={t('public.clubsEmpty')} description={t('public.clubsEmptyLead')} />
      )}

      {!query.isPending && (
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
                  <CardMedia component="img" height={140} image={`/api/files/${club.logoFileId}`} alt="" sx={{ objectFit: 'contain', bgcolor: 'background.default', p: 2 }} />
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
                  {club.clubCategoryName && (
                    <Chip size="small" variant="outlined" color="primary" label={club.clubCategoryName} sx={{ mb: 1 }} />
                  )}
                  <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                    {club.description || t('common.noDescription')}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}
