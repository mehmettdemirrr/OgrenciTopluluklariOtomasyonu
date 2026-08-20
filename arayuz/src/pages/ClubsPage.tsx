import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Snackbar, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useRef, useState, type ChangeEvent } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import type { ClubListItemDto, PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

export function ClubsPage() {
  const queryClient = useQueryClient()
  const { hasPermission } = useAuth()
  const canUploadLogo = hasPermission(Permissions.FilesUpload)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: DEFAULT_PAGE_SIZE,
  })
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)
  const logoInputRef = useRef<HTMLInputElement>(null)
  const [logoTargetClubId, setLogoTargetClubId] = useState<number | null>(null)

  const clubsQuery = useQuery({
    queryKey: ['clubs', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
  })

  const applyMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.post(`/clubs/${clubId}/membership-applications`)
    },
    onSuccess: () => {
      setSnackbar({ message: 'Başvurunuz alındı, danışman onayı bekleniyor.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['membership-applications'] })
    },
    onError: (error) => {
      setSnackbar({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' })
    },
  })

  const logoMutation = useMutation({
    mutationFn: async ({ clubId, file }: { clubId: number; file: File }) => {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post(`/clubs/${clubId}/logo`, formData)
    },
    onSuccess: () => {
      setSnackbar({ message: 'Logo güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => {
      setSnackbar({ message: extractErrorMessage(error, 'Logo yüklenemedi.'), severity: 'error' })
    },
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

  const columns: GridColDef<ClubListItemDto>[] = [
    {
      field: 'logoFileId',
      headerName: 'Logo',
      width: 70,
      sortable: false,
      filterable: false,
      // A-36: açık görsel, anonim uçtan doğrudan <img src> ile — tarayıcı önbelleği çalışır.
      renderCell: (params) =>
        params.row.logoFileId ? (
          <img src={`/api/files/${params.row.logoFileId}`} alt="" height={28} style={{ borderRadius: 4 }} />
        ) : null,
    },
    { field: 'name', headerName: 'Kulüp Adı', flex: 1, minWidth: 200 },
    { field: 'description', headerName: 'Açıklama', flex: 2, minWidth: 240 },
    {
      field: 'isActive',
      headerName: 'Durum',
      width: 120,
      valueFormatter: (value: boolean) => (value ? 'Aktif' : 'Pasif'),
    },
    {
      field: 'actions',
      headerName: '',
      width: canUploadLogo ? 260 : 140,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            size="small"
            variant="outlined"
            disabled={!params.row.isActive || applyMutation.isPending}
            onClick={() => applyMutation.mutate(params.row.id)}
          >
            Başvur
          </Button>
          {canUploadLogo && (
            <Button size="small" disabled={logoMutation.isPending} onClick={() => handleLogoButtonClick(params.row.id)}>
              Logo Yükle
            </Button>
          )}
        </Box>
      ),
    },
  ]

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Kulüpler
      </Typography>

      <input ref={logoInputRef} type="file" accept="image/*" hidden onChange={handleLogoFileChange} />

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={clubsQuery.data?.items ?? []}
          columns={columns}
          loading={clubsQuery.isFetching}
          paginationMode="server"
          rowCount={clubsQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
        />
      </Box>

      <Snackbar
        open={snackbar !== null}
        autoHideDuration={4000}
        onClose={() => setSnackbar(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {snackbar ? <Alert severity={snackbar.severity}>{snackbar.message}</Alert> : undefined}
      </Snackbar>
    </Box>
  )
}
