import { Box, Card, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import { apiClient } from '../../api/client'
import { CardGridSkeleton } from '../../components/ui/CardGridSkeleton'
import { DateBadge } from '../../components/ui/DateBadge'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import { SearchField } from '../../components/ui/SearchField'
import { useSearchPagedQuery } from '../../hooks/useSearchPagedQuery'
import type { PagedResult, PublicEventListItemDto } from '../../api/types'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'

export function PublicEventsPage() {
  useDocumentTitle('Etkinlikler')

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
      <PageHeader title="Etkinlikler" description="Kampüsteki yaklaşan, yayında olan tüm etkinlikler." backTo="/" />

      <Box sx={{ mb: 3 }}>
        <SearchField value={search} onChange={setSearch} placeholder="Etkinlik ara…" />
      </Box>

      {query.isLoading ? (
        <CardGridSkeleton withMedia />
      ) : items.length === 0 ? (
        <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
      ) : (
        <Grid container spacing={2.5}>
          {items.map((event) => (
            <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                {event.posterFileId && (
                  <CardMedia component="img" height={160} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                )}
                <CardContent sx={{ flex: 1 }}>
                  <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 1.5 }}>
                    <DateBadge iso={event.startDateUtc} />
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Typography variant="h6" sx={{ fontWeight: 800, fontSize: 18 }} noWrap>
                        {event.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" noWrap>
                        {event.clubName}
                      </Typography>
                    </Box>
                  </Stack>
                  {event.capacity && <Chip size="small" label={`Kontenjan: ${event.capacity}`} variant="outlined" sx={{ mb: 1 }} />}
                  <Typography variant="body2" sx={{ mb: 0.5 }}>
                    {new Date(event.startDateUtc).toLocaleString('tr-TR')}
                  </Typography>
                  {event.location && (
                    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'text.secondary' }}>
                      <PlaceOutlinedIcon fontSize="inherit" />
                      <Typography variant="caption">{event.location}</Typography>
                    </Stack>
                  )}
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
