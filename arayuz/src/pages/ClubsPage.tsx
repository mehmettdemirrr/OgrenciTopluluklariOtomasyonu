import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Snackbar, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { ClubListItemDto, PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

export function ClubsPage() {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: DEFAULT_PAGE_SIZE,
  })
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

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

  const columns: GridColDef<ClubListItemDto>[] = [
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
      width: 140,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button
          size="small"
          variant="outlined"
          disabled={!params.row.isActive || applyMutation.isPending}
          onClick={() => applyMutation.mutate(params.row.id)}
        >
          Başvur
        </Button>
      ),
    },
  ]

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Kulüpler
      </Typography>

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
