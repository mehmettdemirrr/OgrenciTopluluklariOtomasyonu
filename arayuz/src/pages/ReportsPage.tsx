import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, MenuItem, Select, Stack, Typography } from '@mui/material'
import { BarChart } from '@mui/x-charts/BarChart'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { downloadBlob } from '../api/download'
import { extractErrorMessage } from '../api/errors'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { SectionCard } from '../components/ui/SectionCard'
import { StatCard } from '../components/ui/StatCard'
import { ReportStatusChip } from '../components/ui/StatusChip'
import { brand } from '../theme/tokens'
import type { ClubListItemDto, PagedResult, ReportRequestListItemDto, ReportType, TermSummaryRowDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

const reportTypeLabels: Record<ReportType, string> = {
  ClubMembers: 'Kulüp Üyeleri',
  EventParticipants: 'Etkinlik Katılımcıları',
  TermSummary: 'Dönem Özeti',
}

export function ReportsPage() {
  useDocumentTitle('Raporlarım')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [selectedReportType, setSelectedReportType] = useState<ReportType>('ClubMembers')
  const [selectedClubId, setSelectedClubId] = useState<number | ''>('')

  const summaryQuery = useQuery({
    queryKey: ['reports-summary'],
    queryFn: async () => (await apiClient.get<TermSummaryRowDto[]>('/reports/summary')).data,
  })

  const { paginationModel, setPaginationModel, query: reportsQuery } = usePagedQuery({
    queryKey: ['my-reports'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<ReportRequestListItemDto>>('/reports', { params: { pageIndex, pageSize } })).data,
    // K-04 dışarıda (WebSocket yok) — durum periyodik sorguyla izlenir, bekleyen iş kalmayınca durur.
    refetchInterval: (query) =>
      query.state.data?.items.some((r) => r.status === 'Queued' || r.status === 'Processing') ? 3000 : false,
  })

  const requestMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/reports', {
        reportType: selectedReportType,
        clubId: selectedReportType === 'ClubMembers' ? selectedClubId : undefined,
      })
    },
    onSuccess: () => {
      notify({ message: 'Talebiniz kuyruğa alındı.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['my-reports'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Talep gönderilemedi.'), severity: 'error' }),
  })

  const handleDownload = async (row: ReportRequestListItemDto) => {
    try {
      await downloadBlob(`/reports/${row.id}/file`, `${row.reportType}-${row.id}.xlsx`)
    } catch (error) {
      notify({ message: extractErrorMessage(error, 'Rapor indirilemedi.'), severity: 'error' })
    }
  }

  const columns: GridColDef<ReportRequestListItemDto>[] = [
    { field: 'reportType', headerName: 'Tür', width: 180, valueFormatter: (value: ReportType) => reportTypeLabels[value] },
    {
      field: 'status',
      headerName: 'Durum',
      width: 140,
      renderCell: (params) => <ReportStatusChip status={params.row.status} />,
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

  const summary = summaryQuery.data ?? []
  const latestTerm = summary[0]

  return (
    <>
      <PageHeader title="Raporlarım" description="Dönem özetlerini görüntüleyin, üye ve katılım listelerini Excel olarak indirin." />

      {summary.length > 0 && (
        <>
          <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
            <StatCard label={`${latestTerm.termName} · Kulüp`} value={latestTerm.clubCount} />
            <StatCard label={`${latestTerm.termName} · Üye`} value={latestTerm.memberCount} />
            <StatCard label={`${latestTerm.termName} · Etkinlik`} value={latestTerm.eventCount} />
          </Stack>

          {summary.length > 1 && (
            <SectionCard title="Dönemlere göre karşılaştırma" sx={{ mb: 3 }}>
              <BarChart
                height={280}
                dataset={summary.map((row) => ({ ...row }))}
                xAxis={[{ dataKey: 'termName', scaleType: 'band' }]}
                colors={[brand.turquoise, brand.gold, brand.orange]}
                series={[
                  { dataKey: 'clubCount', label: 'Kulüp' },
                  { dataKey: 'memberCount', label: 'Üye' },
                  { dataKey: 'eventCount', label: 'Etkinlik' },
                ]}
              />
            </SectionCard>
          )}
        </>
      )}

      <SectionCard title="Yeni rapor talebi" sx={{ mb: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <Select
            size="small"
            value={selectedReportType}
            onChange={(e) => setSelectedReportType(e.target.value as ReportType)}
            sx={{ minWidth: 220 }}
          >
            <MenuItem value="ClubMembers">Kulüp Üyeleri (Excel)</MenuItem>
            <MenuItem value="TermSummary">Dönem Özeti (Excel)</MenuItem>
          </Select>

          {selectedReportType === 'ClubMembers' && (
            // A-50/Y-62: kulüp listesi sunucudan aranarak daraltılır — 101. kulüp de seçilebilir.
            <Box sx={{ minWidth: 260 }}>
              <RemoteSelect<ClubListItemDto>
                label="Kulüp"
                size="small"
                value={selectedClubId === '' ? null : selectedClubId}
                onChange={(value) => setSelectedClubId(value ?? '')}
                queryKey={['clubs', 'for-report-form']}
                fetchOptions={async (term) =>
                  (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
                    params: { pageIndex: 0, pageSize: 20, search: term || undefined, isActive: true },
                  })).data.items
                }
                getOptionId={(club) => club.id}
                getOptionLabel={(club) => club.name}
              />
            </Box>
          )}

          <Button
            variant="contained"
            disabled={requestMutation.isPending || (selectedReportType === 'ClubMembers' && selectedClubId === '')}
            onClick={() => requestMutation.mutate()}
          >
            Excel Talep Et
          </Button>
        </Stack>
      </SectionCard>

      <DataTable
        mobileHiddenFields={['requestedAtUtc']}
        rows={reportsQuery.data?.items ?? []}
        columns={columns}
        loading={reportsQuery.isFetching}
        paginationMode="server"
        rowCount={reportsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Henüz rapor talebiniz yok"
        emptyDescription="Yukarıdan bir kulüp seçip Excel talep ederek başlayın."
      />
    </>
  )
}
