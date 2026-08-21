import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  Chip,
  Grid,
  InputAdornment,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useMemo, useRef, useState, type ChangeEvent } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useNotifier } from '../notifications/NotifierProvider'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import type { ClubListItemDto, PagedResult } from '../api/types'

type StatusFilter = 'all' | 'active' | 'inactive'

export function ClubsPage() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canUploadLogo = hasPermission(Permissions.FilesUpload)

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const logoInputRef = useRef<HTMLInputElement>(null)
  const [logoTargetClubId, setLogoTargetClubId] = useState<number | null>(null)

  const clubsQuery = useQuery({
    queryKey: ['clubs', 0, 200],
    queryFn: async () => (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', { params: { pageIndex: 0, pageSize: 200 } })).data,
  })

  const applyMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.post(`/clubs/${clubId}/membership-applications`)
    },
    onSuccess: () => {
      notify({ message: 'Başvurunuz alındı, danışman onayı bekleniyor.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['membership-applications'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const logoMutation = useMutation({
    mutationFn: async ({ clubId, file }: { clubId: number; file: File }) => {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post(`/clubs/${clubId}/logo`, formData)
    },
    onSuccess: () => {
      notify({ message: 'Logo güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Logo yüklenemedi.'), severity: 'error' }),
  })

  const handleLogoButtonClick = (clubId: number) => {
    setLogoTargetClubId(clubId)
    logoInputRef.current?.click()
  }

  const handleLogoFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (file && logoTargetClubId !== null) {
      logoMutation.mutate({ clubId: logoTargetClubId, file })
    }
  }

  const filteredClubs = useMemo(() => {
    const items = clubsQuery.data?.items ?? []
    const query = search.trim().toLocaleLowerCase('tr-TR')
    return items.filter((club) => {
      if (statusFilter === 'active' && !club.isActive) return false
      if (statusFilter === 'inactive' && club.isActive) return false
      if (query && !club.name.toLocaleLowerCase('tr-TR').includes(query)) return false
      return true
    })
  }, [clubsQuery.data, search, statusFilter])

  return (
    <>
      <PageHeader title="Kulüpler" description="Kampüsteki tüm öğrenci topluluklarını keşfedin ve üyelik başvurusu yapın." />

      <input ref={logoInputRef} type="file" accept="image/*" hidden onChange={handleLogoFileChange} />

      <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
        <TextField
          placeholder="Kulüp ara…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ minWidth: 240 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> } }}
        />
        <ToggleButtonGroup exclusive size="small" value={statusFilter} onChange={(_, value: StatusFilter | null) => value && setStatusFilter(value)}>
          <ToggleButton value="all">Tümü</ToggleButton>
          <ToggleButton value="active">Aktif</ToggleButton>
          <ToggleButton value="inactive">Pasif</ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      {!clubsQuery.isLoading && filteredClubs.length === 0 && (
        <EmptyState icon={GroupsOutlinedIcon} title="Kulüp bulunamadı" description="Arama veya filtre kriterlerinizi değiştirmeyi deneyin." />
      )}

      <Grid container spacing={2}>
        {filteredClubs.map((club) => (
          <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              {club.logoFileId ? (
                // A-36: açık görsel, anonim uçtan doğrudan <img src> ile — tarayıcı önbelleği çalışır.
                <CardMedia component="img" height={120} image={`/api/files/${club.logoFileId}`} alt="" sx={{ objectFit: 'contain', bgcolor: 'grey.50', p: 2 }} />
              ) : (
                <Box sx={{ height: 120, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50' }}>
                  <GroupsOutlinedIcon sx={{ fontSize: 40, color: 'grey.400' }} />
                </Box>
              )}
              <CardContent sx={{ flex: 1 }}>
                <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
                    {club.name}
                  </Typography>
                  <Chip size="small" label={club.isActive ? 'Aktif' : 'Pasif'} color={club.isActive ? 'success' : 'default'} variant={club.isActive ? 'filled' : 'outlined'} />
                </Stack>
                <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                  {club.description || 'Açıklama eklenmemiş.'}
                </Typography>
              </CardContent>
              <CardActions sx={{ px: 2, pb: 2 }}>
                <Button
                  size="small"
                  variant="outlined"
                  disabled={!club.isActive || applyMutation.isPending}
                  onClick={() => applyMutation.mutate(club.id)}
                >
                  Başvur
                </Button>
                {canUploadLogo && (
                  <Button size="small" disabled={logoMutation.isPending} onClick={() => handleLogoButtonClick(club.id)}>
                    Logo Yükle
                  </Button>
                )}
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>
    </>
  )
}
