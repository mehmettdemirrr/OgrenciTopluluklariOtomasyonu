import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { SearchField } from '../components/ui/SearchField'
import type { AcademicStaffListItemDto, PagedResult, RegistrationDepartmentDto, UserListItemDto } from '../api/types'

// Y-35: yalnızca biçim — kullanıcının zaten profili olup olmadığı API'nin kararı (A-14).
const createSchema = z.object({
  applicationUserId: z.number({ error: 'Kullanıcı seçin.' }).int().positive('Kullanıcı seçin.'),
  title: z.string().min(1, 'Unvan gerekli.').max(100, 'En fazla 100 karakter.'),
  departmentId: z.number({ error: 'Bölüm seçin.' }).int().positive('Bölüm seçin.'),
})

const updateSchema = createSchema.omit({ applicationUserId: true })

type CreateValues = z.infer<typeof createSchema>
type UpdateValues = z.infer<typeof updateSchema>

function staffName(staff: AcademicStaffListItemDto): string {
  const full = [staff.firstName, staff.lastName].filter(Boolean).join(' ').trim()
  return full === '' ? staff.email : full
}

/**
 * docs/PLAN-V5.md §27.1 (K-33): akademik personel yönetimi.
 *
 * v5.0'a kadar `AcademicStaff` **salt-okunur**du — hiçbir uç oluşturmuyordu, tek kayıt demo
 * seed'inden geliyordu. Yani sisteme yeni danışman eklemek imkânsızdı (bildirilen #3).
 */
export function AcademicStaffTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()

  const [search, setSearch] = useState('')
  const [editTarget, setEditTarget] = useState<AcademicStaffListItemDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AcademicStaffListItemDto | null>(null)
  const createDialog = useFormDialog()

  const debouncedSearch = useDebouncedValue(search)

  const { paginationModel, setPaginationModel, query: staffQuery } = usePagedQuery({
    queryKey: ['academic-staff', 'list', debouncedSearch],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<AcademicStaffListItemDto>>('/academic-staff', {
        params: { pageIndex, pageSize, search: debouncedSearch || undefined },
      })).data,
  })

  const departmentsQuery = useQuery({
    queryKey: ['registration-departments'],
    enabled: createDialog.open || editTarget !== null,
    queryFn: async () => (await apiClient.get<RegistrationDepartmentDto[]>('/auth/departments')).data,
  })

  const createForm = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: { applicationUserId: 0, title: '', departmentId: 0 },
  })

  const editForm = useForm<UpdateValues>({
    resolver: zodResolver(updateSchema),
    defaultValues: { title: '', departmentId: 0 },
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['academic-staff'] })

  const createMutation = useMutation({
    mutationFn: async (values: CreateValues) => {
      await apiClient.post('/academic-staff', values)
    },
    onSuccess: () => {
      notify({ message: 'Akademik personel kaydı oluşturuldu.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset({ applicationUserId: 0, title: '', departmentId: 0 })
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt oluşturulamadı.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: UpdateValues) => {
      if (!editTarget) return
      await apiClient.put(`/academic-staff/${editTarget.id}`, values)
    },
    onSuccess: () => {
      notify({ message: 'Akademik personel kaydı güncellendi.', severity: 'success' })
      setEditTarget(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kayıt güncellenemedi.'), severity: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/academic-staff/${id}`)
    },
    onSuccess: () => {
      notify({ message: 'Akademik personel kaydı silindi.', severity: 'success' })
      setDeleteTarget(null)
      invalidate()
    },
    onError: (error) => {
      // K-33: kulübe danışmanlık yapıyorsa 409 — sebep API'den gelir (Y-35).
      notify({ message: extractErrorMessage(error, 'Kayıt silinemedi.'), severity: 'error' })
      setDeleteTarget(null)
    },
  })

  const openEditDialog = (staff: AcademicStaffListItemDto) => {
    setEditTarget(staff)
    editForm.reset({ title: staff.title, departmentId: 0 })
  }

  const columns: GridColDef<AcademicStaffListItemDto>[] = [
    { field: 'lastName', headerName: 'Ad Soyad', flex: 1, minWidth: 180, sortable: false, valueGetter: (_value, row) => staffName(row) },
    { field: 'title', headerName: 'Unvan', width: 160 },
    { field: 'email', headerName: 'E-posta', flex: 1, minWidth: 220 },
    {
      field: 'actions',
      headerName: '',
      width: 170,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button size="small" variant="outlined" onClick={() => openEditDialog(params.row)}>
            Düzenle
          </Button>
          <Button size="small" color="error" onClick={() => setDeleteTarget(params.row)}>
            Sil
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <>
      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
        <SearchField
          value={search}
          onChange={(value) => {
            setPaginationModel({ ...paginationModel, page: 0 })
            setSearch(value)
          }}
          placeholder="Ad soyad, unvan veya e-posta ara…"
        />
        <Button variant="contained" onClick={createDialog.openDialog}>
          Yeni Akademik Personel
        </Button>
      </Stack>

      <DataTable
        mobileHiddenFields={['email', 'title']}
        rows={staffQuery.data?.items ?? []}
        columns={columns}
        loading={staffQuery.isFetching}
        paginationMode="server"
        rowCount={staffQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Akademik personel bulunamadı"
        emptyDescription="Kulüplere danışman atayabilmek için önce akademik personel kaydı oluşturun."
      />

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          createForm.reset({ applicationUserId: 0, title: '', departmentId: 0 })
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Akademik Personel</DialogTitle>
        <DialogContent>
          <Typography variant="caption" color="text.secondary">
            Var olan bir kullanıcıya akademik personel profili eklenir. Kullanıcı ile birlikte oluşturmak
            için Yetki Matrisi → Kullanıcılar ekranındaki "Yeni Kullanıcı" akışını kullanın.
          </Typography>

          <Controller
            name="applicationUserId"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <RemoteSelect<UserListItemDto>
                label="Kullanıcı"
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? 0)}
                queryKey={['users', 'staff-picker']}
                enabled={createDialog.open}
                fetchOptions={async (term) =>
                  (await apiClient.get<PagedResult<UserListItemDto>>('/users', {
                    params: { pageIndex: 0, pageSize: 20, search: term || undefined },
                  })).data.items
                }
                getOptionId={(user) => user.id}
                getOptionLabel={(user) =>
                  [user.firstName, user.lastName].filter(Boolean).join(' ').trim() || user.email
                }
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />

          <Controller
            name="title"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth margin="dense" label="Akademik Unvan" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />

          <Controller
            name="departmentId"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                select
                fullWidth
                margin="dense"
                label="Bölüm"
                value={field.value || ''}
                onChange={(event) => field.onChange(Number(event.target.value))}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              >
                {(departmentsQuery.data ?? []).map((department) => (
                  <MenuItem key={department.id} value={department.id}>
                    {department.facultyName} — {department.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              createForm.reset({ applicationUserId: 0, title: '', departmentId: 0 })
            }}
          >
            Vazgeç
          </Button>
          <Button variant="contained" disabled={createMutation.isPending} onClick={createForm.handleSubmit((values) => createMutation.mutate(values))}>
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>{editTarget ? staffName(editTarget) : ''} — Düzenle</DialogTitle>
        <DialogContent>
          <Controller
            name="title"
            control={editForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} autoFocus fullWidth margin="dense" label="Akademik Unvan" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="departmentId"
            control={editForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                select
                fullWidth
                margin="dense"
                label="Bölüm"
                value={field.value || ''}
                onChange={(event) => field.onChange(Number(event.target.value))}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              >
                {(departmentsQuery.data ?? []).map((department) => (
                  <MenuItem key={department.id} value={department.id}>
                    {department.facultyName} — {department.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button variant="contained" disabled={updateMutation.isPending} onClick={editForm.handleSubmit((values) => updateMutation.mutate(values))}>
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Akademik personel kaydını sil"
        description={
          deleteTarget
            ? `"${staffName(deleteTarget)}" kaydı silinecek. Bir topluluğa danışmanlık yapıyorsa işlem reddedilir.`
            : undefined
        }
        confirmLabel="Sil"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  )
}
