import { Box, Card, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import { apiClient } from '../../api/client'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ResultPagination } from '../../components/ui/ResultPagination'
import { SearchField } from '../../components/ui/SearchField'
import { useSearchPagedQuery } from '../../hooks/useSearchPagedQuery'
import type { PagedResult, PublicEventListItemDto } from '../../api/types'

export function PublicEventsPage() {
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
      <PageHeader title="Etkinlikler" description="Kampüsteki yaklaşan, yayında olan tüm etkinlikler." />

      <Box sx={{ mb: 3 }}>
        <SearchField value={search} onChange={setSearch} placeholder="Etkinlik ara…" />
      </Box>

      {!query.isLoading && items.length === 0 ? (
        <EmptyState icon={EventOutlinedIcon} title="Yaklaşan etkinlik yok" />
      ) : (
        <Grid container spacing={2}>
          {items.map((event) => (
            <Grid key={event.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%' }}>
                {event.posterFileId && (
                  <CardMedia component="img" height={140} image={`/api/files/${event.posterFileId}`} alt="" sx={{ objectFit: 'cover' }} />
                )}
                <CardContent>
                  <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
                      {event.title}
                    </Typography>
                    {event.capacity && <Chip size="small" label={`Kontenjan: ${event.capacity}`} variant="outlined" />}
                  </Stack>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                    {event.clubName}
                  </Typography>
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
