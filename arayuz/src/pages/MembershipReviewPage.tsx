import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Stack } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { ApplicationStatusChip } from '../components/ui/StatusChip'
import type { MembershipApplicationListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function MembershipReviewPage() {
  useDocumentTitle('Başvuru İncele')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [rejectTarget, setRejectTarget] = useState<MembershipApplicationListItemDto | null>(null)

  const { paginationModel, setPaginationModel, query: applicationsQuery } = usePagedQuery({
    queryKey: ['membership-applications'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<MembershipApplicationListItemDto>>('/membership-applications', { params: { pageIndex, pageSize } })).data,
  })

  const decisionMutation = useMutation({
    mutationFn: async ({ id, status }: { id: number; status: 'Approved' | 'Rejected' }) => {
      await apiClient.put(`/membership-applications/${id}/decision`, { status })
    },
    onSuccess: (_data, variables) => {
      notify({
        message: variables.status === 'Approved' ? 'Başvuru onaylandı, öğrenciye bildirim gönderildi.' : 'Başvuru reddedildi.',
        severity: 'success',
      })
      setRejectTarget(null)
      queryClient.invalidateQueries({ queryKey: ['membership-applications'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<MembershipApplicationListItemDto>[] = [
    { field: 'clubName', headerName: 'Kulüp', flex: 1, minWidth: 180 },
    { field: 'studentNumber', headerName: 'Öğrenci No', width: 140 },
    {
      field: 'status',
      headerName: 'Durum',
      width: 140,
      renderCell: (params) => <ApplicationStatusChip status={params.row.status} />,
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
            onClick={() => setRejectTarget(params.row)}
          >
            Reddet
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Üyelik Başvuruları" description="Kulüplerinize gelen üyelik başvurularını onaylayın veya reddedin." />

      <DataTable
        mobileHiddenFields={['appliedAtUtc']}
        rows={applicationsQuery.data?.items ?? []}
        columns={columns}
        loading={applicationsQuery.isFetching}
        paginationMode="server"
        rowCount={applicationsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Bekleyen başvuru yok"
        emptyDescription="Kulüplerinize yeni bir üyelik başvurusu geldiğinde burada görünecek."
      />

      <ConfirmDialog
        open={rejectTarget !== null}
        title="Başvuruyu reddet"
        description={rejectTarget ? `${rejectTarget.studentNumber} numaralı öğrencinin ${rejectTarget.clubName} başvurusunu reddetmek istediğinize emin misiniz?` : undefined}
        confirmLabel="Reddet"
        destructive
        loading={decisionMutation.isPending}
        onConfirm={() => rejectTarget && decisionMutation.mutate({ id: rejectTarget.id, status: 'Rejected' })}
        onCancel={() => setRejectTarget(null)}
      />
    </>
  )
}
