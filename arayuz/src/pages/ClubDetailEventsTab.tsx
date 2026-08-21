import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { EventStatusChip } from '../components/ui/StatusChip'
import type { EventListItemDto, PagedResult } from '../api/types'

export function ClubEventsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canWrite = hasPermission(Permissions.EventsWrite)

  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [startDateTime, setStartDateTime] = useState('')
  const [endDateTime, setEndDateTime] = useState('')

  const { paginationModel, setPaginationModel, query: eventsQuery } = usePagedQuery({
    queryKey: ['club-events', clubId],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<EventListItemDto>>(`/clubs/${clubId}/events`, { params: { pageIndex, pageSize } })).data,
  })

  const createEventMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post(`/clubs/${clubId}/events`, {
        title,
        startDateUtc: new Date(startDateTime).toISOString(),
        endDateUtc: new Date(endDateTime).toISOString(),
      })
    },
    onSuccess: () => {
      notify({ message: 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      setCreateDialogOpen(false)
      setTitle('')
      setStartDateTime('')
      setEndDateTime('')
      queryClient.invalidateQueries({ queryKey: ['club-events', clubId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik oluşturulamadı.'), severity: 'error' }),
  })

  const columns: GridColDef<EventListItemDto>[] = [
    { field: 'title', headerName: 'Başlık', flex: 1, minWidth: 200 },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    { field: 'status', headerName: 'Durum', width: 150, renderCell: (params) => <EventStatusChip status={params.row.status} /> },
    {
      field: 'actions',
      headerName: '',
      width: 120,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" component={RouterLink} to={`/events/${params.row.id}`}>
          Detay
        </Button>
      ),
    },
  ]

  return (
    <>
      {canWrite && (
        <Stack direction="row" sx={{ mb: 2 }}>
          <Button variant="contained" onClick={() => setCreateDialogOpen(true)}>
            Etkinlik Oluştur
          </Button>
        </Stack>
      )}

      <DataTable
        rows={eventsQuery.data?.items ?? []}
        columns={columns}
        loading={eventsQuery.isFetching}
        paginationMode="server"
        rowCount={eventsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Bu toplulukta etkinlik yok"
      />

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Etkinlik</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Başlık" value={title} onChange={(e) => setTitle(e.target.value)} />
          <TextField
            fullWidth
            margin="dense"
            label="Başlangıç"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            value={startDateTime}
            onChange={(e) => setStartDateTime(e.target.value)}
          />
          <TextField
            fullWidth
            margin="dense"
            label="Bitiş"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            value={endDateTime}
            onChange={(e) => setEndDateTime(e.target.value)}
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
    </>
  )
}
