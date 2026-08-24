import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, Stack, TextField } from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { ApplicationStatusChip } from '../components/ui/StatusChip'
import {
  clubApplicationDecisionFormSchema,
  emptyClubApplicationDecisionFormValues,
  type ClubApplicationDecisionFormValues,
} from '../schemas/clubApplicationDecisionForm'
import type { ClubApplicationListItemDto, PagedResult } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function ClubApplicationsReviewPage() {
  useDocumentTitle('Topluluk Kurma Başvuruları')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [rejectTarget, setRejectTarget] = useState<ClubApplicationListItemDto | null>(null)

  const rejectForm = useForm<ClubApplicationDecisionFormValues>({
    resolver: zodResolver(clubApplicationDecisionFormSchema),
    defaultValues: emptyClubApplicationDecisionFormValues,
  })

  const { paginationModel, setPaginationModel, query: applicationsQuery } = usePagedQuery({
    queryKey: ['club-applications'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<ClubApplicationListItemDto>>('/club-applications', { params: { pageIndex, pageSize } })).data,
  })

  const decisionMutation = useMutation({
    mutationFn: async ({ id, status, reviewNote }: { id: number; status: 'Approved' | 'Rejected'; reviewNote?: string }) => {
      await apiClient.put(`/club-applications/${id}/decision`, { status, reviewNote: reviewNote?.trim() || null })
    },
    onSuccess: (_data, variables) => {
      notify({
        message: variables.status === 'Approved' ? 'Başvuru onaylandı, topluluk oluşturuldu.' : 'Başvuru reddedildi.',
        severity: 'success',
      })
      setRejectTarget(null)
      rejectForm.reset(emptyClubApplicationDecisionFormValues)
      queryClient.invalidateQueries({ queryKey: ['club-applications'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<ClubApplicationListItemDto>[] = [
    { field: 'proposedName', headerName: 'Önerilen Ad', flex: 1, minWidth: 180 },
    { field: 'studentNumber', headerName: 'Öğrenci No', width: 130 },
    { field: 'proposedAdvisorTitle', headerName: 'Önerilen Danışman', flex: 1, minWidth: 180 },
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
      <PageHeader title="Topluluk Kurma Başvuruları" description="Öğrencilerin topluluk kurma başvurularını onaylayın veya reddedin." />

      <DataTable
        mobileHiddenFields={['proposedAdvisorTitle', 'appliedAtUtc']}
        rows={applicationsQuery.data?.items ?? []}
        columns={columns}
        getRowHeight={() => 'auto'}
        loading={applicationsQuery.isFetching}
        paginationMode="server"
        rowCount={applicationsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Bekleyen başvuru yok"
        emptyDescription="Yeni bir topluluk kurma başvurusu geldiğinde burada görünecek."
      />

      <Dialog
        open={rejectTarget !== null}
        onClose={() => {
          setRejectTarget(null)
          rejectForm.reset(emptyClubApplicationDecisionFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Başvuruyu Reddet</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 1 }}>
            {rejectTarget && `"${rejectTarget.proposedName}" başvurusunu reddetmek istediğinize emin misiniz?`}
          </DialogContentText>
          <Controller
            name="reviewNote"
            control={rejectForm.control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={2} margin="dense" label="Ret gerekçesi (opsiyonel)" />}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setRejectTarget(null)
              rejectForm.reset(emptyClubApplicationDecisionFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={decisionMutation.isPending}
            onClick={rejectForm.handleSubmit((values) =>
              rejectTarget && decisionMutation.mutate({ id: rejectTarget.id, status: 'Rejected', reviewNote: values.reviewNote }),
            )}
          >
            Reddet
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
