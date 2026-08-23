import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'
import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { useState } from 'react'
import { Controller, useForm, type Control } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import {
  academicTermFormSchema,
  emptyAcademicTermFormValues,
  emptyNameFormValues,
  nameFormSchema,
  type AcademicTermFormValues,
  type NameFormValues,
} from '../schemas/referenceForm'
import type { AcademicTermListItemDto, DepartmentListItemDto, FacultyListItemDto, PagedResult } from '../api/types'

function NameFormField({ control, label }: { control: Control<NameFormValues>; label: string }) {
  return (
    <Controller
      name="name"
      control={control}
      render={({ field, fieldState }) => (
        <TextField {...field} autoFocus fullWidth margin="dense" label={label} error={!!fieldState.error} helperText={fieldState.error?.message} />
      )}
    />
  )
}

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
  const facultyDialog = useFormDialog()
  const departmentDialog = useFormDialog()
  const [editFacultyTarget, setEditFacultyTarget] = useState<FacultyListItemDto | null>(null)
  const [editDepartmentTarget, setEditDepartmentTarget] = useState<DepartmentListItemDto | null>(null)
  const [deleteDepartmentTarget, setDeleteDepartmentTarget] = useState<DepartmentListItemDto | null>(null)

  const createFacultyForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })
  const createDepartmentForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })
  const editFacultyForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })
  const editDepartmentForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })

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
    mutationFn: async (values: NameFormValues) => (await apiClient.post<FacultyListItemDto>('/faculties', { name: values.name })).data,
    onSuccess: (_data, values) => {
      notify({ message: `"${values.name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      facultyDialog.closeDialog()
      createFacultyForm.reset(emptyNameFormValues)
      queryClient.invalidateQueries({ queryKey: ['faculties'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Fakülte eklenemedi.'), severity: 'error' }),
  })

  const createDepartmentMutation = useMutation({
    mutationFn: async (values: NameFormValues) => {
      if (selectedFacultyId === null) throw new Error('Önce bir fakülte seçin.')
      return (await apiClient.post<DepartmentListItemDto>(`/faculties/${selectedFacultyId}/departments`, { name: values.name })).data
    },
    onSuccess: (_data, values) => {
      notify({ message: `"${values.name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      departmentDialog.closeDialog()
      createDepartmentForm.reset(emptyNameFormValues)
      queryClient.invalidateQueries({ queryKey: ['departments', selectedFacultyId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Bölüm eklenemedi.'), severity: 'error' }),
  })

  const updateFacultyMutation = useMutation({
    mutationFn: async (values: NameFormValues) => {
      if (!editFacultyTarget) return
      await apiClient.put(`/faculties/${editFacultyTarget.id}`, { name: values.name })
    },
    onSuccess: () => {
      notify({ message: 'Fakülte güncellendi.', severity: 'success' })
      setEditFacultyTarget(null)
      queryClient.invalidateQueries({ queryKey: ['faculties'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Fakülte güncellenemedi.'), severity: 'error' }),
  })

  const updateDepartmentMutation = useMutation({
    mutationFn: async (values: NameFormValues) => {
      if (selectedFacultyId === null || !editDepartmentTarget) return
      await apiClient.put(`/faculties/${selectedFacultyId}/departments/${editDepartmentTarget.id}`, { name: values.name })
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
            editFacultyForm.reset({ name: params.row.name })
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
              editDepartmentForm.reset({ name: params.row.name })
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
          <Button variant="contained" size="small" onClick={facultyDialog.openDialog}>
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
            <Button variant="outlined" size="small" onClick={departmentDialog.openDialog}>
              Yeni Bölüm
            </Button>
          }
        >
          <Box sx={{ height: 300 }}>
            <DataGrid rows={departmentsQuery.data?.items ?? []} columns={departmentColumns} loading={departmentsQuery.isFetching} hideFooter sx={{ border: 'none' }} />
          </Box>
        </SectionCard>
      )}

      <Dialog
        open={facultyDialog.open}
        onClose={() => {
          facultyDialog.closeDialog()
          createFacultyForm.reset(emptyNameFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Fakülte</DialogTitle>
        <DialogContent>
          <NameFormField control={createFacultyForm.control} label="Fakülte Adı" />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              facultyDialog.closeDialog()
              createFacultyForm.reset(emptyNameFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createFacultyForm.formState.isSubmitting || createFacultyMutation.isPending}
            onClick={createFacultyForm.handleSubmit((values) => createFacultyMutation.mutate(values))}
          >
            Ekle
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={departmentDialog.open}
        onClose={() => {
          departmentDialog.closeDialog()
          createDepartmentForm.reset(emptyNameFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Bölüm</DialogTitle>
        <DialogContent>
          <NameFormField control={createDepartmentForm.control} label="Bölüm Adı" />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              departmentDialog.closeDialog()
              createDepartmentForm.reset(emptyNameFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createDepartmentForm.formState.isSubmitting || createDepartmentMutation.isPending}
            onClick={createDepartmentForm.handleSubmit((values) => createDepartmentMutation.mutate(values))}
          >
            Ekle
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editFacultyTarget !== null} onClose={() => setEditFacultyTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Fakülteyi Düzenle</DialogTitle>
        <DialogContent>
          <NameFormField control={editFacultyForm.control} label="Fakülte Adı" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditFacultyTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editFacultyForm.formState.isSubmitting || updateFacultyMutation.isPending}
            onClick={editFacultyForm.handleSubmit((values) => updateFacultyMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editDepartmentTarget !== null} onClose={() => setEditDepartmentTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Bölümü Düzenle</DialogTitle>
        <DialogContent>
          <NameFormField control={editDepartmentForm.control} label="Bölüm Adı" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDepartmentTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editDepartmentForm.formState.isSubmitting || updateDepartmentMutation.isPending}
            onClick={editDepartmentForm.handleSubmit((values) => updateDepartmentMutation.mutate(values))}
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
  const dialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<AcademicTermListItemDto | null>(null)

  const createForm = useForm<AcademicTermFormValues>({ resolver: zodResolver(academicTermFormSchema), defaultValues: emptyAcademicTermFormValues })
  const editForm = useForm<AcademicTermFormValues>({ resolver: zodResolver(academicTermFormSchema), defaultValues: emptyAcademicTermFormValues })

  const { paginationModel, setPaginationModel, query: termsQuery } = usePagedQuery({
    queryKey: ['academic-terms'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AcademicTermListItemDto>>('/academic-terms', { params: { pageIndex, pageSize } })).data,
  })

  const createTermMutation = useMutation({
    mutationFn: async (values: AcademicTermFormValues) => {
      await apiClient.post('/academic-terms', {
        name: values.name,
        startDateUtc: new Date(values.startDate).toISOString(),
        endDateUtc: new Date(values.endDate).toISOString(),
      })
    },
    onSuccess: () => {
      notify({ message: 'Dönem oluşturuldu.', severity: 'success' })
      dialog.closeDialog()
      createForm.reset(emptyAcademicTermFormValues)
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
    mutationFn: async (values: AcademicTermFormValues) => {
      if (editTarget === null) throw new Error('Düzenlenecek dönem seçilmedi.')
      await apiClient.put(`/academic-terms/${editTarget.id}`, {
        name: values.name,
        startDateUtc: new Date(values.startDate).toISOString(),
        endDateUtc: new Date(values.endDate).toISOString(),
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
    editForm.reset({ name: term.name, startDate: term.startDateUtc.slice(0, 10), endDate: term.endDateUtc.slice(0, 10) })
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
        <Button variant="contained" onClick={dialog.openDialog}>
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

      <Dialog
        open={dialog.open}
        onClose={() => {
          dialog.closeDialog()
          createForm.reset(emptyAcademicTermFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Akademik Dönem</DialogTitle>
        <DialogContent>
          <AcademicTermFormFields control={createForm.control} />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              dialog.closeDialog()
              createForm.reset(emptyAcademicTermFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createForm.formState.isSubmitting || createTermMutation.isPending}
            onClick={createForm.handleSubmit((values) => createTermMutation.mutate(values))}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Akademik Dönemi Düzenle</DialogTitle>
        <DialogContent>
          <AcademicTermFormFields control={editForm.control} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={editForm.formState.isSubmitting || updateTermMutation.isPending}
            onClick={editForm.handleSubmit((values) => updateTermMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

function AcademicTermFormFields({ control }: { control: Control<AcademicTermFormValues> }) {
  return (
    <>
      <Controller
        name="name"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="dense" label="Dönem Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="startDate"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            fullWidth
            margin="dense"
            label="Başlangıç Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
          />
        )}
      />
      <Controller
        name="endDate"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            fullWidth
            margin="dense"
            label="Bitiş Tarihi"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
          />
        )}
      />
    </>
  )
}
