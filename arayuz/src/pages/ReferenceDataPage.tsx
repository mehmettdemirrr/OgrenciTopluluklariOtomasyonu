import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Snackbar,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel, type GridRowSelectionModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { AcademicTermListItemDto, DepartmentListItemDto, FacultyListItemDto, PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

type Snack = { message: string; severity: 'success' | 'error' } | null

export function ReferenceDataPage() {
  const [tab, setTab] = useState(0)
  const [snackbar, setSnackbar] = useState<Snack>(null)

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Referans Verisi
      </Typography>

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Fakülte / Bölüm" />
        <Tab label="Akademik Dönemler" />
      </Tabs>

      {tab === 0 && <FacultiesTab onNotify={setSnackbar} />}
      {tab === 1 && <TermsTab onNotify={setSnackbar} />}

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

function FacultiesTab({ onNotify }: { onNotify: (snack: Snack) => void }) {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [selectedFacultyId, setSelectedFacultyId] = useState<number | null>(null)
  const [facultyDialogOpen, setFacultyDialogOpen] = useState(false)
  const [newFacultyName, setNewFacultyName] = useState('')
  const [departmentDialogOpen, setDepartmentDialogOpen] = useState(false)
  const [newDepartmentName, setNewDepartmentName] = useState('')

  const facultiesQuery = useQuery({
    queryKey: ['faculties', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<FacultyListItemDto>>('/faculties', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
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
      onNotify({ message: `"${name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      setFacultyDialogOpen(false)
      setNewFacultyName('')
      queryClient.invalidateQueries({ queryKey: ['faculties'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Fakülte eklenemedi.'), severity: 'error' }),
  })

  const createDepartmentMutation = useMutation({
    mutationFn: async (name: string) => {
      if (selectedFacultyId === null) throw new Error('Önce bir fakülte seçin.')
      return (await apiClient.post<DepartmentListItemDto>(`/faculties/${selectedFacultyId}/departments`, { name })).data
    },
    onSuccess: (_data, name) => {
      onNotify({ message: `"${name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      setDepartmentDialogOpen(false)
      setNewDepartmentName('')
      queryClient.invalidateQueries({ queryKey: ['departments', selectedFacultyId] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Bölüm eklenemedi.'), severity: 'error' }),
  })

  const facultyColumns: GridColDef<FacultyListItemDto>[] = [{ field: 'name', headerName: 'Fakülte Adı', flex: 1, minWidth: 220 }]
  const departmentColumns: GridColDef<DepartmentListItemDto>[] = [{ field: 'name', headerName: 'Bölüm Adı', flex: 1, minWidth: 220 }]

  const handleSelectionChange = (model: GridRowSelectionModel) => {
    const ids = Array.from(model.ids)
    setSelectedFacultyId(ids.length > 0 ? Number(ids[0]) : null)
  }

  return (
    <Box>
      <Box sx={{ mb: 1 }}>
        <Button variant="contained" onClick={() => setFacultyDialogOpen(true)}>
          Yeni Fakülte
        </Button>
      </Box>

      <Typography variant="subtitle2" gutterBottom>
        Fakülteler (bölümleri görmek için bir satır seçin)
      </Typography>
      <Box sx={{ height: 300, mb: 3 }}>
        <DataGrid
          rows={facultiesQuery.data?.items ?? []}
          columns={facultyColumns}
          loading={facultiesQuery.isFetching}
          paginationMode="server"
          rowCount={facultiesQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          checkboxSelection={false}
          onRowSelectionModelChange={handleSelectionChange}
        />
      </Box>

      {selectedFacultyId !== null && (
        <>
          <Box sx={{ mb: 1 }}>
            <Button variant="outlined" onClick={() => setDepartmentDialogOpen(true)}>
              Yeni Bölüm
            </Button>
          </Box>
          <Typography variant="subtitle2" gutterBottom>
            Bölümler
          </Typography>
          <Box sx={{ height: 300 }}>
            <DataGrid
              rows={departmentsQuery.data?.items ?? []}
              columns={departmentColumns}
              loading={departmentsQuery.isFetching}
              hideFooter
            />
          </Box>
        </>
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
    </Box>
  )
}

function TermsTab({ onNotify }: { onNotify: (snack: Snack) => void }) {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const termsQuery = useQuery({
    queryKey: ['academic-terms', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<AcademicTermListItemDto>>('/academic-terms', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
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
      onNotify({ message: 'Dönem oluşturuldu.', severity: 'success' })
      setDialogOpen(false)
      setName('')
      setStartDate('')
      setEndDate('')
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Dönem oluşturulamadı.'), severity: 'error' }),
  })

  const setCurrentMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.put(`/academic-terms/${id}/current`)
    },
    onSuccess: () => {
      onNotify({ message: 'Güncel dönem güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Güncel dönem güncellenemedi.'), severity: 'error' }),
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
    <Box>
      <Box sx={{ mb: 2 }}>
        <Button variant="contained" onClick={() => setDialogOpen(true)}>
          Yeni Dönem
        </Button>
      </Box>

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={termsQuery.data?.items ?? []}
          columns={columns}
          loading={termsQuery.isFetching}
          paginationMode="server"
          rowCount={termsQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
        />
      </Box>

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
    </Box>
  )
}
