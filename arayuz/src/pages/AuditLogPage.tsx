import { useState } from 'react'
import { Chip, Stack, TextField, Typography } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { apiClient } from '../api/client'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { AuditAction, AuditLogListItemDto, PagedResult } from '../api/types'

const actionLabels: Record<AuditAction, { label: string; color: 'success' | 'info' | 'error' }> = {
  Insert: { label: 'Oluşturma', color: 'success' },
  Update: { label: 'Güncelleme', color: 'info' },
  Delete: { label: 'Silme', color: 'error' },
}

export function AuditLogPage() {
  const [entityType, setEntityType] = useState('')
  const [entityId, setEntityId] = useState('')
  const [userId, setUserId] = useState('')

  const { paginationModel, setPaginationModel, query: auditLogsQuery } = usePagedQuery({
    queryKey: ['audit-logs', entityType, entityId, userId],
    queryFn: async (pageIndex, pageSize) =>
      (
        await apiClient.get<PagedResult<AuditLogListItemDto>>('/audit-logs', {
          params: {
            entityType: entityType.trim() || undefined,
            entityId: entityId.trim() || undefined,
            userId: userId.trim() === '' ? undefined : Number(userId),
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
      <PageHeader title="Denetim İzi" description="Sistem genelindeki oluşturma/güncelleme/silme işlemlerinin kaydı." />

      <SectionCard sx={{ mb: 3 }}>
        <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: 'block' }}>
          Y-26: parola/token/stamp gibi hassas alanlar bu kayıtlarda hiç görünmez.
        </Typography>
        <Stack direction="row" spacing={2} sx={{ flexWrap: 'wrap' }}>
          <TextField size="small" label="Varlık türü" placeholder="ör. Club" value={entityType} onChange={(event) => setEntityType(event.target.value)} />
          <TextField size="small" label="Kayıt Id" value={entityId} onChange={(event) => setEntityId(event.target.value)} />
          <TextField
            size="small"
            label="Kullanıcı Id"
            value={userId}
            onChange={(event) => setUserId(event.target.value.replace(/\D/g, ''))}
          />
        </Stack>
      </SectionCard>

      <DataTable
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
