import { useState } from 'react'
import { Button, Chip, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { apiClient } from '../api/client'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { AuditAction, AuditLogListItemDto, PagedResult, TrafficLogListItemDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

const actionLabels: Record<AuditAction, { label: string; color: 'success' | 'info' | 'error' }> = {
  Insert: { label: 'Oluşturma', color: 'success' },
  Update: { label: 'Güncelleme', color: 'info' },
  Delete: { label: 'Silme', color: 'error' },
}

export function AuditLogPage() {
  useDocumentTitle('Denetim İzi')

  const [tab, setTab] = useState(0)
  const [correlationFilter, setCorrelationFilter] = useState<string | null>(null)

  const goToRelatedChanges = (correlationId: string) => {
    setCorrelationFilter(correlationId)
    setTab(0)
  }

  return (
    <>
      <PageHeader title="Denetim İzi" description="Veri değişiklikleri ve istek bazlı erişim izi." />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Veri Değişiklikleri" />
        <Tab label="Erişim İzi" />
      </Tabs>

      {tab === 0 && <DataChangesTab correlationFilter={correlationFilter} onClearCorrelationFilter={() => setCorrelationFilter(null)} />}
      {tab === 1 && <TrafficTab onShowRelatedChanges={goToRelatedChanges} />}
    </>
  )
}

function DataChangesTab({
  correlationFilter,
  onClearCorrelationFilter,
}: {
  correlationFilter: string | null
  onClearCorrelationFilter: () => void
}) {
  const [entityType, setEntityType] = useState('')
  const [entityId, setEntityId] = useState('')
  const [userId, setUserId] = useState('')

  const { paginationModel, setPaginationModel, query: auditLogsQuery } = usePagedQuery({
    queryKey: ['audit-logs', entityType, entityId, userId, correlationFilter],
    queryFn: async (pageIndex, pageSize) =>
      (
        await apiClient.get<PagedResult<AuditLogListItemDto>>('/audit-logs', {
          params: {
            entityType: entityType.trim() || undefined,
            entityId: entityId.trim() || undefined,
            userId: userId.trim() === '' ? undefined : Number(userId),
            correlationId: correlationFilter ?? undefined,
            pageIndex,
            pageSize,
          },
        })
      ).data,
  })

  const columns: GridColDef<AuditLogListItemDto>[] = [
    {
      field: 'timestampUtc',
      headerName: 'Zaman',
      width: 180,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    { field: 'entityType', headerName: 'Varlık', width: 160 },
    { field: 'entityId', headerName: 'Kayıt Id', width: 100 },
    {
      field: 'action',
      headerName: 'İşlem',
      width: 130,
      renderCell: (params) => {
        const entry = actionLabels[params.row.action]
        return <Chip size="small" label={entry.label} color={entry.color} variant="outlined" />
      },
    },
    {
      field: 'userId',
      headerName: 'Kullanıcı Id',
      width: 120,
      valueFormatter: (value: number | null) => value ?? '—',
    },
  ]

  return (
    <>
      <SectionCard sx={{ mb: 3 }}>
        <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: 'block' }}>
          Y-26: parola/token/stamp gibi hassas alanlar bu kayıtlarda hiç görünmez.
        </Typography>
        <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField size="small" label="Varlık türü" placeholder="ör. Club" value={entityType} onChange={(event) => setEntityType(event.target.value)} />
          <TextField size="small" label="Kayıt Id" value={entityId} onChange={(event) => setEntityId(event.target.value)} />
          <TextField
            size="small"
            label="Kullanıcı Id"
            value={userId}
            onChange={(event) => setUserId(event.target.value.replace(/\D/g, ''))}
          />
          {correlationFilter && (
            <Chip
              label="Yalnızca seçili istekle ilişkili kayıtlar"
              onDelete={onClearCorrelationFilter}
              color="primary"
              variant="outlined"
              size="small"
            />
          )}
        </Stack>
      </SectionCard>

      <DataTable
        mobileHiddenFields={['entityId', 'userId']}
        rows={auditLogsQuery.data?.items ?? []}
        columns={columns}
        loading={auditLogsQuery.isFetching}
        paginationMode="server"
        rowCount={auditLogsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Kayıt bulunamadı"
      />
    </>
  )
}

function TrafficTab({ onShowRelatedChanges }: { onShowRelatedChanges: (correlationId: string) => void }) {
  const [userId, setUserId] = useState('')
  const [ipAddress, setIpAddress] = useState('')
  const [httpMethod, setHttpMethod] = useState('')

  const { paginationModel, setPaginationModel, query: trafficLogsQuery } = usePagedQuery({
    queryKey: ['traffic-logs', userId, ipAddress, httpMethod],
    queryFn: async (pageIndex, pageSize) =>
      (
        await apiClient.get<PagedResult<TrafficLogListItemDto>>('/traffic-logs', {
          params: {
            userId: userId.trim() === '' ? undefined : Number(userId),
            ipAddress: ipAddress.trim() || undefined,
            httpMethod: httpMethod.trim() || undefined,
            pageIndex,
            pageSize,
          },
        })
      ).data,
  })

  const columns: GridColDef<TrafficLogListItemDto>[] = [
    {
      field: 'timestampUtc',
      headerName: 'Zaman',
      width: 170,
      valueFormatter: (value: string) => new Date(value).toLocaleString('tr-TR'),
    },
    { field: 'userId', headerName: 'Kullanıcı Id', width: 110, valueFormatter: (value: number | null) => value ?? '—' },
    { field: 'ipAddress', headerName: 'IP', width: 140 },
    { field: 'httpMethod', headerName: 'Yöntem', width: 90 },
    {
      field: 'path',
      headerName: 'URL',
      flex: 1,
      minWidth: 220,
      renderCell: (params) => `${params.row.path}${params.row.redactedQueryString ?? ''}`,
    },
    {
      field: 'statusCode',
      headerName: 'Durum',
      width: 90,
      renderCell: (params) => (
        <Chip size="small" label={params.row.statusCode} color={params.row.statusCode >= 400 ? 'error' : 'success'} variant="outlined" />
      ),
    },
    { field: 'durationMs', headerName: 'Süre (ms)', width: 100 },
    {
      field: 'actions',
      headerName: '',
      width: 190,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" onClick={() => onShowRelatedChanges(params.row.correlationId)}>
          İlişkili Değişiklikler
        </Button>
      ),
    },
  ]

  return (
    <>
      <SectionCard sx={{ mb: 3 }}>
        <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: 'block' }}>
          Y-59: istek/cevap gövdesi ve header'lar hiç kaydedilmez; sorgu dizesindeki bilinen hassas anahtarlar (ör. access_token) redakte edilir.
          Yalnızca yazma istekleri, giriş/çıkış uçları ve hatalı (4xx/5xx) istekler tutulur — başarılı okuma istekleri kaydedilmez.
        </Typography>
        <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
          <TextField
            size="small"
            label="Kullanıcı Id"
            value={userId}
            onChange={(event) => setUserId(event.target.value.replace(/\D/g, ''))}
          />
          <TextField size="small" label="IP" value={ipAddress} onChange={(event) => setIpAddress(event.target.value)} />
          <TextField size="small" label="Yöntem" placeholder="ör. PUT" value={httpMethod} onChange={(event) => setHttpMethod(event.target.value)} />
        </Stack>
      </SectionCard>

      <DataTable
        mobileHiddenFields={['durationMs', 'ipAddress', 'statusCode']}
        rows={trafficLogsQuery.data?.items ?? []}
        columns={columns}
        loading={trafficLogsQuery.isFetching}
        paginationMode="server"
        rowCount={trafficLogsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Kayıt bulunamadı"
      />
    </>
  )
}
