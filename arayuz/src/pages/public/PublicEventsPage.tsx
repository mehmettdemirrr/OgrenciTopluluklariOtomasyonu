import { Box, Chip, Grid } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import { apiClient } from '../../api/client'
import { EventCard } from '../../components/events/EventCard'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import { SearchField } from '../../components/ui/SearchField'
import { useSearchPagedQuery } from '../../hooks/useSearchPagedQuery'
import type { PagedResult, PublicEventListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'

export function PublicEventsPage() {
  const { t } = useLocale()
  useDocumentTitle(t('public.eventsTitle'))

  const { search, setSearch, items, pageIndex, setPageIndex, pageCount, totalCount, query } =
    useSearchPagedQuery<PublicEventListItemDto>({
      queryKey: ['public-events'],
      queryFn: async ({ pageIndex: page, pageSize, search: term }) =>
        (await apiClient.get<PagedResult<PublicEventListItemDto>>('/public/events', {
          params: { pageIndex: page, pageSize, search: term || undefined },
        })).data,
    })

  return (
    <>
      <PageHeader title={t('public.eventsTitle')} description={t('public.eventsLead')} backTo="/" />

      <Box sx={{ mb: 3 }}>
        <SearchField value={search} onChange={setSearch} placeholder={t('common.searchEvents')} />
      </Box>

      {query.isLoading ? (
        <CardGridSkeleton withMedia />
      ) : items.length === 0 ? (
        <EmptyState icon={EventOutlinedIcon} title={t('home.noEvents')} />
      ) : (
        <Grid container spacing={2.5}>
          {items.map((event) => (
            <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <EventCard
                to={`/etkinlikler/${event.id}`}
                title={event.title}
                clubName={event.clubName}
                startDateUtc={event.startDateUtc}
                location={event.location}
                posterFileId={event.posterFileId}
                badges={event.capacity ? <Chip size="small" variant="outlined" label={t('common.capacity', { count: event.capacity })} /> : undefined}
              />
            </Grid>
          ))}
        </Grid>
      )}

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />
    </>
  )
}
