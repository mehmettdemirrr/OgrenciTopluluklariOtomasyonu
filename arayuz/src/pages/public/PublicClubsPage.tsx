import { useQuery } from '@tanstack/react-query'
import { Box, Card, CardContent, CardMedia, Grid, InputAdornment, TextField, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import type { PagedResult, PublicClubListItemDto } from '../../api/types'

export function PublicClubsPage() {
  const [search, setSearch] = useState('')

  const clubsQuery = useQuery({
    queryKey: ['public-clubs', 0, 200],
    queryFn: async () => (await apiClient.get<PagedResult<PublicClubListItemDto>>('/public/clubs', { params: { pageIndex: 0, pageSize: 200 } })).data,
  })

  const filteredClubs = useMemo(() => {
    const items = clubsQuery.data?.items ?? []
    const query = search.trim().toLowerCase()
    return query === '' ? items : items.filter((club) => club.name.toLowerCase().includes(query))
  }, [clubsQuery.data, search])

  return (
    <>
      <PageHeader title="Kulüpler" description="Kampüsteki aktif toplulukları keşfedin." />

      <TextField
        placeholder="Kulüp ara…"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        sx={{ mb: 3, minWidth: 260 }}
        slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> } }}
      />

      {!clubsQuery.isLoading && filteredClubs.length === 0 && (
        <EmptyState icon={GroupsOutlinedIcon} title="Kulüp bulunamadı" description="Arama kriterinizi değiştirmeyi deneyin." />
      )}

      <Grid container spacing={2}>
        {filteredClubs.map((club) => (
          <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card
              variant="outlined"
              component={RouterLink}
              to={`/kulupler/${club.id}`}
              sx={{ display: 'block', height: '100%', textDecoration: 'none', color: 'inherit' }}
            >
              {club.logoFileId ? (
                <CardMedia component="img" height={120} image={`/api/files/${club.logoFileId}`} alt="" sx={{ objectFit: 'contain', bgcolor: 'grey.50', p: 2 }} />
              ) : (
                <Box sx={{ height: 120, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50' }}>
                  <GroupsOutlinedIcon sx={{ fontSize: 40, color: 'grey.400' }} />
                </Box>
              )}
              <CardContent>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 0.5 }} noWrap>
                  {club.name}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                  {club.description || 'Açıklama eklenmemiş.'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </>
  )
}
