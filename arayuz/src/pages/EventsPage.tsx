import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import type { ClubListItemDto, EventListItemDto, PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

type Snack = { message: string; severity: 'success' | 'error' } | null

export function EventsPage() {
  const { hasPermission } = useAuth()
  const [tab, setTab] = useState(0)
  const [snackbar, setSnackbar] = useState<Snack>(null)
  const canApprove = hasPermission(Permissions.EventsApprove)

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Etkinlikler
      </Typography>

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Etkinlikler" />
        {canApprove && <Tab label="Onay Kuyruğu" />}
      </Tabs>

      {tab === 0 && <EventsTab onNotify={setSnackbar} />}
      {tab === 1 && canApprove && <ApprovalQueueTab onNotify={setSnackbar} />}

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

function EventsTab({ onNotify }: { onNotify: (snack: Snack) => void }) {
  const queryClient = useQueryClient()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.EventsWrite)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [selectedClubId, setSelectedClubId] = useState<number | ''>('')
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [startDateTime, setStartDateTime] = useState('')
  const [endDateTime, setEndDateTime] = useState('')

  const clubsQuery = useQuery({
    queryKey: ['clubs', 0, 200],
    queryFn: async () => (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', { params: { pageIndex: 0, pageSize: 200 } })).data,
  })

  const eventsQuery = useQuery({
    queryKey: ['events', selectedClubId, paginationModel.page, paginationModel.pageSize],
    enabled: selectedClubId !== '',
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<EventListItemDto>>('/events', {
        params: { clubId: selectedClubId, pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
  })

  const createEventMutation = useMutation({
    mutationFn: async () => {
      if (selectedClubId === '') throw new Error('Önce bir topluluk seçin.')
      await apiClient.post(`/clubs/${selectedClubId}/events`, {
        title,
        startDateUtc: new Date(startDateTime).toISOString(),
        endDateUtc: new Date(endDateTime).toISOString(),
      })
    },
    onSuccess: () => {
      onNotify({ message: 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      setCreateDialogOpen(false)
      setTitle('')
      setStartDateTime('')
      setEndDateTime('')
      queryClient.invalidateQueries({ queryKey: ['events'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Etkinlik oluşturulamadı.'), severity: 'error' }),
  })

  const submitMutation = useMutation({
    mutationFn: async (eventId: number) => {
      await apiClient.put(`/events/${eventId}/submission`)
    },
    onSuccess: () => {
      onNotify({ message: 'Etkinlik onaya gönderildi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Onaya gönderilemedi.'), severity: 'error' }),
  })

  const statusLabels: Record<EventListItemDto['status'], string> = {
    Draft: 'Taslak',
    PendingApproval: 'Onay Bekliyor',
    Published: 'Yayında',
    Rejected: 'Reddedildi',
  }

  const columns: GridColDef<EventListItemDto>[] = [
    { field: 'title', headerName: 'Başlık', flex: 1, minWidth: 200 },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    { field: 'status', headerName: 'Durum', width: 130, valueFormatter: (value: EventListItemDto['status']) => statusLabels[value] },
    ...(canWrite
      ? [
          {
            field: 'actions',
            headerName: '',
            width: 160,
            sortable: false,
            filterable: false,
            renderCell: (params: { row: EventListItemDto }) => (
              <Button
                size="small"
                variant="outlined"
                disabled={params.row.status !== 'Draft' || submitMutation.isPending}
                onClick={() => submitMutation.mutate(params.row.id)}
              >
                Onaya Gönder
              </Button>
            ),
          } satisfies GridColDef<EventListItemDto>,
        ]
      : []),
  ]

  return (
    <Box>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 2 }}>
        <TextField
          select
          size="small"
          label="Topluluk"
          value={selectedClubId}
          onChange={(event) => setSelectedClubId(event.target.value === '' ? '' : Number(event.target.value))}
          sx={{ width: 280 }}
        >
          {(clubsQuery.data?.items ?? []).map((club) => (
            <MenuItem key={club.id} value={club.id}>
              {club.name}
            </MenuItem>
          ))}
        </TextField>

        {canWrite && (
          <Button variant="contained" disabled={selectedClubId === ''} onClick={() => setCreateDialogOpen(true)}>
            Etkinlik Oluştur
          </Button>
        )}
      </Stack>

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={eventsQuery.data?.items ?? []}
          columns={columns}
          loading={eventsQuery.isFetching}
          paginationMode="server"
          rowCount={eventsQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
        />
      </Box>

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Etkinlik</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Başlık" value={title} onChange={(event) => setTitle(event.target.value)} />
          <TextField
            fullWidth
            margin="dense"
            label="Başlangıç"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            value={startDateTime}
            onChange={(event) => setStartDateTime(event.target.value)}
          />
          <TextField
            fullWidth
            margin="dense"
            label="Bitiş"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            value={endDateTime}
            onChange={(event) => setEndDateTime(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={title.trim() === '' || !startDateTime || !endDateTime || createEventMutation.isPending}
            onClick={() => createEventMutation.mutate()}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function ApprovalQueueTab({ onNotify }: { onNotify: (snack: Snack) => void }) {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })

  const queueQuery = useQuery({
    queryKey: ['events-approval-queue', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<EventListItemDto>>('/events/approval-queue', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
  })

  const decideMutation = useMutation({
    mutationFn: async ({ id, status }: { id: number; status: 'Published' | 'Rejected' }) => {
      await apiClient.put(`/events/${id}/decision`, { status })
    },
    onSuccess: (_data, variables) => {
      onNotify({ message: variables.status === 'Published' ? 'Etkinlik yayınlandı.' : 'Etkinlik reddedildi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events-approval-queue'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<EventListItemDto>[] = [
    { field: 'clubName', headerName: 'Topluluk', flex: 1, minWidth: 160 },
    { field: 'title', headerName: 'Başlık', flex: 1, minWidth: 200 },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 170,
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
            disabled={decideMutation.isPending}
            onClick={() => decideMutation.mutate({ id: params.row.id, status: 'Published' })}
          >
            Onayla
          </Button>
          <Button
            size="small"
            variant="outlined"
            color="error"
            disabled={decideMutation.isPending}
            onClick={() => decideMutation.mutate({ id: params.row.id, status: 'Rejected' })}
          >
            Reddet
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <Box sx={{ height: 480 }}>
      <DataGrid
        rows={queueQuery.data?.items ?? []}
        columns={columns}
        loading={queueQuery.isFetching}
        paginationMode="server"
        rowCount={queueQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        disableRowSelectionOnClick
      />
    </Box>
  )
}
