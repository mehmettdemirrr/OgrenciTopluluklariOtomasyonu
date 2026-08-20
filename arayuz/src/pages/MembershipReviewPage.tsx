import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Chip, Snackbar, Stack, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { ApplicationStatus, MembershipApplicationListItemDto, PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

const statusLabels: Record<ApplicationStatus, string> = {
  Pending: 'Bekliyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
}

const statusColors: Record<ApplicationStatus, 'warning' | 'success' | 'error'> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'error',
}

export function MembershipReviewPage() {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: DEFAULT_PAGE_SIZE,
  })
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)

  const applicationsQuery = useQuery({
    queryKey: ['membership-applications', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<MembershipApplicationListItemDto>>('/membership-applications', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
  })

  const decisionMutation = useMutation({
    mutationFn: async ({ id, status }: { id: number; status: 'Approved' | 'Rejected' }) => {
      await apiClient.put(`/membership-applications/${id}/decision`, { status })
    },
    onSuccess: (_data, variables) => {
      setSnackbar({
        message: variables.status === 'Approved' ? 'Başvuru onaylandı, öğrenciye bildirim gönderildi.' : 'Başvuru reddedildi.',
        severity: 'success',
      })
      queryClient.invalidateQueries({ queryKey: ['membership-applications'] })
    },
    onError: (error) => {
      setSnackbar({ message: extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'), severity: 'error' })
    },
  })

  const columns: GridColDef<MembershipApplicationListItemDto>[] = [
    { field: 'clubName', headerName: 'Kulüp', flex: 1, minWidth: 180 },
    { field: 'studentNumber', headerName: 'Öğrenci No', width: 140 },
    {
      field: 'status',
      headerName: 'Durum',
      width: 130,
      renderCell: (params) => <Chip size="small" label={statusLabels[params.row.status]} color={statusColors[params.row.status]} />,
    },
    {
      field: 'appliedAtUtc',
      headerName: 'Başvuru Tarihi',
      width: 180,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    {
      field: 'actions',
      headerName: '',
      width: 220,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button
            size="small"
            variant="contained"
            color="success"
            disabled={params.row.status !== 'Pending' || decisionMutation.isPending}
            onClick={() => decisionMutation.mutate({ id: params.row.id, status: 'Approved' })}
          >
            Onayla
          </Button>
          <Button
            size="small"
            variant="outlined"
            color="error"
            disabled={params.row.status !== 'Pending' || decisionMutation.isPending}
            onClick={() => decisionMutation.mutate({ id: params.row.id, status: 'Rejected' })}
          >
            Reddet
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Üyelik Başvuruları
      </Typography>

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={applicationsQuery.data?.items ?? []}
          columns={columns}
          loading={applicationsQuery.isFetching}
          paginationMode="server"
          rowCount={applicationsQuery.data?.totalCount ?? 0}
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
