import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
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
  const [editFacultyTarget, setEditFacultyTarget] = useState<FacultyListItemDto | null>(null)
  const [editFacultyName, setEditFacultyName] = useState('')
  const [editDepartmentTarget, setEditDepartmentTarget] = useState<DepartmentListItemDto | null>(null)
  const [editDepartmentName, setEditDepartmentName] = useState('')
  const [deleteDepartmentTarget, setDeleteDepartmentTarget] = useState<DepartmentListItemDto | null>(null)

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

  const updateFacultyMutation = useMutation({
    mutationFn: async ({ id, name }: { id: number; name: string }) => {
      await apiClient.put(`/faculties/${id}`, { name })
    },
    onSuccess: () => {
      notify({ message: 'Fakülte güncellendi.', severity: 'success' })
      setEditFacultyTarget(null)
      queryClient.invalidateQueries({ queryKey: ['faculties'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Fakülte güncellenemedi.'), severity: 'error' }),
  })

  const updateDepartmentMutation = useMutation({
    mutationFn: async ({ id, name }: { id: number; name: string }) => {
      if (selectedFacultyId === null) throw new Error('Önce bir fakülte seçin.')
      await apiClient.put(`/faculties/${selectedFacultyId}/departments/${id}`, { name })
    },
    onSuccess: () => {
      notify({ message: 'Bölüm güncellendi.', severity: 'success' })
      setEditDepartmentTarget(null)
      queryClient.invalidateQueries({ queryKey: ['departments', selectedFacultyId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Bölüm güncellenemedi.'), severity: 'error' }),
  })

  const deleteDepartmentMutation = useMutation({
    mutationFn: async (id: number) => {
      if (selectedFacultyId === null) throw new Error('Önce bir fakülte seçin.')
      await apiClient.delete(`/faculties/${selectedFacultyId}/departments/${id}`)
    },
    onSuccess: () => {
      notify({ message: 'Bölüm silindi.', severity: 'success' })
      setDeleteDepartmentTarget(null)
      queryClient.invalidateQueries({ queryKey: ['departments', selectedFacultyId] })
    },
    onError: (error) => {
      notify({ message: extractErrorMessage(error, 'Bölüm silinemedi.'), severity: 'error' })
      setDeleteDepartmentTarget(null)
    },
  })

  const facultyColumns: GridColDef<FacultyListItemDto>[] = [
    { field: 'name', headerName: 'Fakülte Adı', flex: 1, minWidth: 220 },
    {
      field: 'actions',
      headerName: '',
      width: 64,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <IconButton
          size="small"
          onClick={(event) => {
            event.stopPropagation()
            setEditFacultyTarget(params.row)
            setEditFacultyName(params.row.name)
          }}
        >
          <EditOutlinedIcon fontSize="small" />
        </IconButton>
      ),
    },
  ]
  const departmentColumns: GridColDef<DepartmentListItemDto>[] = [
    { field: 'name', headerName: 'Bölüm Adı', flex: 1, minWidth: 220 },
    {
      field: 'actions',
      headerName: '',
      width: 96,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation()
              setEditDepartmentTarget(params.row)
              setEditDepartmentName(params.row.name)
            }}
          >
            <EditOutlinedIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation()
              setDeleteDepartmentTarget(params.row)
            }}
          >
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Stack>
      ),
    },
  ]

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

      <Dialog open={editFacultyTarget !== null} onClose={() => setEditFacultyTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Fakülteyi Düzenle</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Fakülte Adı"
            value={editFacultyName}
            onChange={(event) => setEditFacultyName(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditFacultyTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editFacultyName.trim() === '' || updateFacultyMutation.isPending}
            onClick={() => editFacultyTarget && updateFacultyMutation.mutate({ id: editFacultyTarget.id, name: editFacultyName.trim() })}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editDepartmentTarget !== null} onClose={() => setEditDepartmentTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Bölümü Düzenle</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Bölüm Adı"
            value={editDepartmentName}
            onChange={(event) => setEditDepartmentName(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDepartmentTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editDepartmentName.trim() === '' || updateDepartmentMutation.isPending}
            onClick={() => editDepartmentTarget && updateDepartmentMutation.mutate({ id: editDepartmentTarget.id, name: editDepartmentName.trim() })}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteDepartmentTarget !== null}
        title="Bölümü sil"
        description={`"${deleteDepartmentTarget?.name}" bölümünü silmek istediğinize emin misiniz? Kayıtlı öğrencisi varsa silme işlemi reddedilir.`}
        confirmLabel="Sil"
        destructive
        loading={deleteDepartmentMutation.isPending}
        onCancel={() => setDeleteDepartmentTarget(null)}
        onConfirm={() => deleteDepartmentTarget && deleteDepartmentMutation.mutate(deleteDepartmentTarget.id)}
      />
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
  const [editTarget, setEditTarget] = useState<AcademicTermListItemDto | null>(null)
  const [editName, setEditName] = useState('')
  const [editStartDate, setEditStartDate] = useState('')
  const [editEndDate, setEditEndDate] = useState('')

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

  const updateTermMutation = useMutation({
    mutationFn: async () => {
      if (editTarget === null) throw new Error('Düzenlenecek dönem seçilmedi.')
      await apiClient.put(`/academic-terms/${editTarget.id}`, {
        name: editName,
        startDateUtc: new Date(editStartDate).toISOString(),
        endDateUtc: new Date(editEndDate).toISOString(),
      })
    },
    onSuccess: () => {
      notify({ message: 'Akademik dönem güncellendi.', severity: 'success' })
      setEditTarget(null)
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Dönem güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (term: AcademicTermListItemDto) => {
    setEditTarget(term)
    setEditName(term.name)
    setEditStartDate(term.startDateUtc.slice(0, 10))
    setEditEndDate(term.endDateUtc.slice(0, 10))
  }

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
      width: 210,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <IconButton size="small" onClick={() => openEditDialog(params.row)}>
            <EditOutlinedIcon fontSize="small" />
          </IconButton>
          <Button
            size="small"
            variant="outlined"
            disabled={params.row.isCurrent || setCurrentMutation.isPending}
            onClick={() => setCurrentMutation.mutate(params.row.id)}
          >
            Güncel Yap
          </Button>
        </Stack>
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

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Akademik Dönemi Düzenle</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Dönem Adı" value={editName} onChange={(event) => setEditName(event.target.value)} />
          <TextField
            fullWidth
            margin="dense"
            label="Başlangıç Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={editStartDate}
            onChange={(event) => setEditStartDate(event.target.value)}
          />
          <TextField
            fullWidth
            margin="dense"
            label="Bitiş Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={editEndDate}
            onChange={(event) => setEditEndDate(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editName.trim() === '' || !editStartDate || !editEndDate || updateTermMutation.isPending}
            onClick={() => updateTermMutation.mutate()}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
