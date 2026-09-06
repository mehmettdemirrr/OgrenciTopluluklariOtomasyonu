import { Button, Stack } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { DataTable } from '../components/ui/DataTable'
import { EventAudienceChip, EventStatusChip } from '../components/ui/StatusChip'
import type { EventListItemDto, PagedResult } from '../api/types'

/**
 * docs/MIMARI.md · A-75/Y-81: `canManage` çağıranın BU kulüpteki `EventsManage` kapasitesidir
 * (ClubDetailPage'den gelir) — global `events.write` izni değil. Yetkisizken yönetim ucu
 * (`GET /api/clubs/{id}/events`) hiç çağrılmaz, yayınlanmış etkinlik ucu kullanılır (K-45).
 */
export function ClubEventsTab({ clubId, canManage }: { clubId: number; canManage: boolean }) {
  const { paginationModel, setPaginationModel, query: eventsQuery } = usePagedQuery({
    queryKey: ['club-events', clubId, canManage],
    queryFn: async (pageIndex, pageSize) =>
      canManage
        ? (await apiClient.get<PagedResult<EventListItemDto>>(`/clubs/${clubId}/events`, { params: { pageIndex, pageSize } })).data
        : (await apiClient.get<PagedResult<EventListItemDto>>('/events', { params: { clubId, pageIndex, pageSize } })).data,
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
    { field: 'audience', headerName: 'Kitle', width: 130, renderCell: (params) => <EventAudienceChip audience={params.row.audience} /> },
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
      {canManage && (
        <Stack direction="row" sx={{ mb: 2 }}>
          <Button variant="contained" component={RouterLink} to={`/events/new?clubId=${clubId}&returnTo=/clubs/${clubId}`}>
            Etkinlik Oluştur
          </Button>
        </Stack>
      )}

      <DataTable
        mobileHiddenFields={['startDateUtc', 'audience']}
        rows={eventsQuery.data?.items ?? []}
        columns={columns}
        loading={eventsQuery.isFetching}
        paginationMode="server"
        rowCount={eventsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle={canManage ? 'Bu toplulukta etkinlik yok' : 'Bu toplulukta yayınlanmış etkinlik yok'}
      />
    </>
  )
}
