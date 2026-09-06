import { Alert, Grid } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { useAuth } from '../../auth/AuthContext'
import { ClubBrowseCard } from '../../components/clubs/ClubBrowseCard'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import type { PagedResult, PublicClubCategoryDto, PublicClubListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import { useNotifier } from '../../notifications/NotifierProvider'
import { ClubBrowseFilters } from './ClubBrowseFilters'

const PAGE_SIZE = 12

export function PublicClubsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('public.clubsTitle'))
  const { isAuthenticated } = useAuth()
  const notify = useNotifier()
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

  // Y-86: uygunluk kararı sunucudadır; buradaki tek iş isteği göndermek ve dönen mesajı göstermek.
  const joinMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.post(`/clubs/${clubId}/membership-applications`)
    },
    onSuccess: () => notify({ message: 'Başvurunuz alındı, danışman onayı bekleniyor.', severity: 'success' }),
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const handleShare = async (club: PublicClubListItemDto) => {
    const url = `${window.location.origin}/kulupler/${club.id}`
    if (navigator.share) {
      await navigator.share({ title: club.name, url }).catch(() => undefined)
      return
    }
    await navigator.clipboard.writeText(url).catch(() => undefined)
    notify({ message: 'Bağlantı kopyalandı.', severity: 'success' })
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
              <ClubBrowseCard
                club={club}
                isAuthenticated={isAuthenticated}
                joining={joinMutation.isPending}
                onJoin={(clubId) => joinMutation.mutate(clubId)}
                onShare={handleShare}
              />
            </Grid>
          ))}
        </Grid>
      )}

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}
