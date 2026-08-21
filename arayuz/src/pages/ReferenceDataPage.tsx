import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Tab, Tabs, TextField, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { AcademicTermListItemDto, DepartmentListItemDto, FacultyListItemDto, PagedResult } from '../api/types'

export function ReferenceDataPage() {
  const [tab, setTab] = useState(0)

  return (
    <>
      <PageHeader title="Referans Verisi" description="Fakülte, bölüm ve akademik dönem verilerini yönetin." />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Fakülte / Bölüm" />
        <Tab label="Akademik Dönemler" />
      </Tabs>

      {tab === 0 && <FacultiesTab />}
      {tab === 1 && <TermsTab />}
    </>
  )
}

function FacultiesTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [selectedFacultyId, setSelectedFacultyId] = useState<number | null>(null)
  const [facultyDialogOpen, setFacultyDialogOpen] = useState(false)
  const [newFacultyName, setNewFacultyName] = useState('')
  const [departmentDialogOpen, setDepartmentDialogOpen] = useState(false)
  const [newDepartmentName, setNewDepartmentName] = useState('')

  const { paginationModel, setPaginationModel, query: facultiesQuery } = usePagedQuery({
    queryKey: ['faculties'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<FacultyListItemDto>>('/faculties', { params: { pageIndex, pageSize } })).data,
  })

  const departmentsQuery = useQuery({
    queryKey: ['departments', selectedFacultyId],
    enabled: selectedFacultyId !== null,
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<DepartmentListItemDto>>(`/faculties/${selectedFacultyId}/departments`, {
        params: { pageIndex: 0, pageSize: 200 },
      })
      return response.data
    },
  })

  const createFacultyMutation = useMutation({
    mutationFn: async (name: string) => (await apiClient.post<FacultyListItemDto>('/faculties', { name })).data,
    onSuccess: (_data, name) => {
      notify({ message: `"${name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      setFacultyDialogOpen(false)
      setNewFacultyName('')
      queryClient.invalidateQueries({ queryKey: ['faculties'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Fakülte eklenemedi.'), severity: 'error' }),
  })

  const createDepartmentMutation = useMutation({
    mutationFn: async (name: string) => {
      if (selectedFacultyId === null) throw new Error('Önce bir fakülte seçin.')
      return (await apiClient.post<DepartmentListItemDto>(`/faculties/${selectedFacultyId}/departments`, { name })).data
    },
    onSuccess: (_data, name) => {
      notify({ message: `"${name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      setDepartmentDialogOpen(false)
      setNewDepartmentName('')
      queryClient.invalidateQueries({ queryKey: ['departments', selectedFacultyId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Bölüm eklenemedi.'), severity: 'error' }),
  })

  const facultyColumns: GridColDef<FacultyListItemDto>[] = [{ field: 'name', headerName: 'Fakülte Adı', flex: 1, minWidth: 220 }]
  const departmentColumns: GridColDef<DepartmentListItemDto>[] = [{ field: 'name', headerName: 'Bölüm Adı', flex: 1, minWidth: 220 }]

  const handleSelectionChange = (model: GridRowSelectionModel) => {
    const ids = Array.from(model.ids)
    setSelectedFacultyId(ids.length > 0 ? Number(ids[0]) : null)
  }

  return (
    <>
      <SectionCard
        title="Fakülteler"
        action={
          <Button variant="contained" size="small" onClick={() => setFacultyDialogOpen(true)}>
            Yeni Fakülte
          </Button>
        }
        sx={{ mb: 3 }}
      >
        <Typography variant="caption" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
          Bölümleri görmek için bir satır seçin.
        </Typography>
        <Box sx={{ height: 300 }}>
          <DataGrid
            rows={facultiesQuery.data?.items ?? []}
            columns={facultyColumns}
            loading={facultiesQuery.isFetching}
            paginationMode="server"
            rowCount={facultiesQuery.data?.totalCount ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            pageSizeOptions={[10, 20, 50]}
            disableRowSelectionOnClick={false}
            onRowSelectionModelChange={handleSelectionChange}
            sx={{ border: 'none' }}
          />
        </Box>
      </SectionCard>

      {selectedFacultyId !== null && (
        <SectionCard
          title="Bölümler"
          action={
            <Button variant="outlined" size="small" onClick={() => setDepartmentDialogOpen(true)}>
              Yeni Bölüm
            </Button>
          }
        >
          <Box sx={{ height: 300 }}>
            <DataGrid rows={departmentsQuery.data?.items ?? []} columns={departmentColumns} loading={departmentsQuery.isFetching} hideFooter sx={{ border: 'none' }} />
          </Box>
        </SectionCard>
      )}

      <Dialog open={facultyDialogOpen} onClose={() => setFacultyDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Fakülte</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Fakülte Adı"
            value={newFacultyName}
            onChange={(event) => setNewFacultyName(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setFacultyDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={newFacultyName.trim() === '' || createFacultyMutation.isPending}
            onClick={() => createFacultyMutation.mutate(newFacultyName.trim())}
          >
            Ekle
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={departmentDialogOpen} onClose={() => setDepartmentDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Bölüm</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Bölüm Adı"
            value={newDepartmentName}
            onChange={(event) => setNewDepartmentName(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDepartmentDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={newDepartmentName.trim() === '' || createDepartmentMutation.isPending}
            onClick={() => createDepartmentMutation.mutate(newDepartmentName.trim())}
          >
            Ekle
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

function TermsTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const { paginationModel, setPaginationModel, query: termsQuery } = usePagedQuery({
    queryKey: ['academic-terms'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AcademicTermListItemDto>>('/academic-terms', { params: { pageIndex, pageSize } })).data,
  })

  const createTermMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/academic-terms', {
        name,
        startDateUtc: new Date(startDate).toISOString(),
        endDateUtc: new Date(endDate).toISOString(),
      })
    },
    onSuccess: () => {
      notify({ message: 'Dönem oluşturuldu.', severity: 'success' })
      setDialogOpen(false)
      setName('')
      setStartDate('')
      setEndDate('')
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Dönem oluşturulamadı.'), severity: 'error' }),
  })

  const setCurrentMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.put(`/academic-terms/${id}/current`)
    },
    onSuccess: () => {
      notify({ message: 'Güncel dönem güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Güncel dönem güncellenemedi.'), severity: 'error' }),
  })

  const columns: GridColDef<AcademicTermListItemDto>[] = [
    { field: 'name', headerName: 'Dönem', flex: 1, minWidth: 160 },
    {
      field: 'startDateUtc',
      headerName: 'Başlangıç',
      width: 140,
      valueFormatter: (value: string) => new Date(value).toLocaleDateString('tr-TR'),
    },
    {
      field: 'endDateUtc',
      headerName: 'Bitiş',
      width: 140,
      valueFormatter: (value: string) => new Date(value).toLocaleDateString('tr-TR'),
    },
    {
      field: 'isCurrent',
      headerName: 'Güncel',
      width: 110,
      renderCell: (params) => (params.row.isCurrent ? <Chip size="small" color="success" label="Güncel" /> : null),
    },
    {
      field: 'actions',
      headerName: '',
      width: 160,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button
          size="small"
          variant="outlined"
          disabled={params.row.isCurrent || setCurrentMutation.isPending}
          onClick={() => setCurrentMutation.mutate(params.row.id)}
        >
          Güncel Yap
        </Button>
      ),
    },
  ]

  return (
    <>
      <Box sx={{ mb: 2 }}>
        <Button variant="contained" onClick={() => setDialogOpen(true)}>
          Yeni Dönem
        </Button>
      </Box>

      <DataTable
        rows={termsQuery.data?.items ?? []}
        columns={columns}
        loading={termsQuery.isFetching}
        paginationMode="server"
        rowCount={termsQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Akademik dönem yok"
      />

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Akademik Dönem</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Dönem Adı" value={name} onChange={(event) => setName(event.target.value)} />
          <TextField
            fullWidth
            margin="dense"
            label="Başlangıç Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={startDate}
            onChange={(event) => setStartDate(event.target.value)}
          />
          <TextField
            fullWidth
            margin="dense"
            label="Bitiş Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={endDate}
            onChange={(event) => setEndDate(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={name.trim() === '' || !startDate || !endDate || createTermMutation.isPending}
            onClick={() => createTermMutation.mutate()}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
