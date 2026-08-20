import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Card, CardContent, Chip, MenuItem, Select, Snackbar, Stack, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { downloadBlob } from '../api/download'
import { extractErrorMessage } from '../api/errors'
import type { ClubListItemDto, PagedResult, ReportRequestListItemDto, ReportStatus, ReportType, TermSummaryRowDto } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

const statusLabels: Record<ReportStatus, string> = {
  Queued: 'Kuyrukta',
  Processing: 'Üretiliyor',
  Ready: 'Hazır',
  Failed: 'Hatalı',
}

const statusColors: Record<ReportStatus, 'default' | 'warning' | 'success' | 'error'> = {
  Queued: 'default',
  Processing: 'warning',
  Ready: 'success',
  Failed: 'error',
}

const reportTypeLabels: Record<ReportType, string> = {
  ClubMembers: 'Kulüp Üyeleri',
  EventParticipants: 'Etkinlik Katılımcıları',
}

export function ReportsPage() {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [snackbar, setSnackbar] = useState<{ message: string; severity: 'success' | 'error' } | null>(null)
  const [selectedClubId, setSelectedClubId] = useState<number | ''>('')

  const summaryQuery = useQuery({
    queryKey: ['reports-summary'],
    queryFn: async () => (await apiClient.get<TermSummaryRowDto[]>('/reports/summary')).data,
  })

  const clubsQuery = useQuery({
    queryKey: ['clubs', 'for-report-form'],
    queryFn: async () =>
      (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })

  const reportsQuery = useQuery({
    queryKey: ['my-reports', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<ReportRequestListItemDto>>('/reports', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
    // K-04 dışarıda (WebSocket yok) — durum periyodik sorguyla izlenir, bekleyen iş kalmayınca durur.
    refetchInterval: (query) =>
      query.state.data?.items.some((r) => r.status === 'Queued' || r.status === 'Processing') ? 3000 : false,
  })

  const requestMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/reports', { reportType: 'ClubMembers', clubId: selectedClubId });
    },
    onSuccess: () => {
      setSnackbar({ message: 'Talebiniz kuyruğa alındı.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['my-reports'] })
    },
    onError: (error) => {
      setSnackbar({ message: extractErrorMessage(error, 'Talep gönderilemedi.'), severity: 'error' })
    },
  })

  const handleDownload = async (row: ReportRequestListItemDto) => {
    try {
      await downloadBlob(`/reports/${row.id}/file`, `${row.reportType}-${row.id}.xlsx`)
    } catch (error) {
      setSnackbar({ message: extractErrorMessage(error, 'Rapor indirilemedi.'), severity: 'error' })
    }
  }

  const columns: GridColDef<ReportRequestListItemDto>[] = [
    { field: 'reportType', headerName: 'Tür', width: 180, valueFormatter: (value: ReportType) => reportTypeLabels[value] },
    {
      field: 'status',
      headerName: 'Durum',
      width: 130,
      renderCell: (params) => <Chip size="small" label={statusLabels[params.row.status]} color={statusColors[params.row.status]} />,
    },
    {
      field: 'requestedAtUtc',
      headerName: 'Talep Tarihi',
      width: 180,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    {
      field: 'actions',
      headerName: '',
      width: 220,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        if (params.row.status === 'Ready' && params.row.hasFile) {
          return (
            <Button size="small" variant="outlined" onClick={() => handleDownload(params.row)}>
              İndir
            </Button>
          )
        }

        if (params.row.status === 'Failed') {
          return (
            <Typography variant="caption" color="error">
              {params.row.errorMessage ?? 'Üretim başarısız.'}
            </Typography>
          )
        }

        return null
      },
    },
  ]

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Raporlarım
      </Typography>

      {summaryQuery.data && summaryQuery.data.length > 0 && (
        <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
          {summaryQuery.data.map((row) => (
            <Card key={row.academicTermId} variant="outlined" sx={{ minWidth: 220 }}>
              <CardContent>
                <Typography variant="subtitle2" color="text.secondary">
                  {row.termName}
                </Typography>
                <Typography variant="body2">Kulüp: {row.clubCount}</Typography>
                <Typography variant="body2">Üye: {row.memberCount}</Typography>
                <Typography variant="body2">Etkinlik: {row.eventCount}</Typography>
              </CardContent>
            </Card>
          ))}
        </Stack>
      )}

      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 3 }}>
        <Select size="small" value="ClubMembers" sx={{ minWidth: 220 }} disabled>
          <MenuItem value="ClubMembers">Kulüp Üyeleri (Excel)</MenuItem>
        </Select>

        <Select
          size="small"
          displayEmpty
          value={selectedClubId}
          onChange={(e) => {
            const raw = String(e.target.value)
            setSelectedClubId(raw === '' ? '' : Number(raw))
          }}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">
            <em>Kulüp seçin</em>
          </MenuItem>
          {clubsQuery.data?.items.map((club) => (
            <MenuItem key={club.id} value={club.id}>
              {club.name}
            </MenuItem>
          ))}
        </Select>

        <Button variant="contained" disabled={requestMutation.isPending || selectedClubId === ''} onClick={() => requestMutation.mutate()}>
          Excel Talep Et
        </Button>
      </Stack>

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={reportsQuery.data?.items ?? []}
          columns={columns}
          loading={reportsQuery.isFetching}
          paginationMode="server"
          rowCount={reportsQuery.data?.totalCount ?? 0}
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
