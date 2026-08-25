import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SearchField } from '../components/ui/SearchField'
import { createUserFormSchema, emptyCreateUserFormValues, type CreateUserFormValues } from '../schemas/createUserForm'
import type { PagedResult, RegistrationDepartmentDto, RoleListItemDto, UserListItemDto } from '../api/types'

/** Y-67: bu roller domain profili ister — seçilince ek alanlar zorunlu olur. */
const STUDENT_ROLE = 'Member'
const ADVISOR_ROLE = 'Advisor'

function displayName(user: UserListItemDto): string {
  const full = [user.firstName, user.lastName].filter(Boolean).join(' ').trim()
  return full === '' ? '—' : full
}

/**
 * docs/PLAN-V5.md §26.3 (K-32): kullanıcı yönetimi.
 *
 * v5.0'a kadar bu ekran yalnızca e-posta ve rolleri gösteriyordu; `POST /users` ve
 * `PUT /users/{id}/lockout` uçları backend'de **arayüzsüz** duruyordu (bulgu 11).
 * Faz 29'da "Yetki Matrisi"nin ikinci sekmesi olmaktan çıkıp kendi rotasına taşındı.
 */
export function UsersPage() {
  useDocumentTitle('Kullanıcılar')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { email: currentEmail } = useAuth()

  const [search, setSearch] = useState('')
  const [editingUser, setEditingUser] = useState<UserListItemDto | null>(null)
  const [editingRoleNames, setEditingRoleNames] = useState<Set<string>>(new Set())
  const [deleteTarget, setDeleteTarget] = useState<UserListItemDto | null>(null)
  const createDialog = useFormDialog()

  const debouncedSearch = useDebouncedValue(search)

  const { paginationModel, setPaginationModel, query: usersQuery } = usePagedQuery({
    queryKey: ['users', debouncedSearch],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<UserListItemDto>>('/users', { params: { pageIndex, pageSize, search: debouncedSearch || undefined } })).data,
  })

  // Rol atama diyaloğu doğası gereği TÜM rolleri ister (checkbox listesi) — Y-62'nin istisnası:
  // sınırlı ve yönetici tarafından tanımlanan bir küme okunur, istemci filtrelemesi yok.
  const rolesQuery = useQuery({
    queryKey: ['roles', 'all'],
    queryFn: async () => (await apiClient.get<PagedResult<RoleListItemDto>>('/roles', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })

  const departmentsQuery = useQuery({
    queryKey: ['registration-departments'],
    enabled: createDialog.open,
    queryFn: async () => (await apiClient.get<RegistrationDepartmentDto[]>('/auth/departments')).data,
  })

  const createForm = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserFormSchema),
    defaultValues: emptyCreateUserFormValues,
  })

  // `watch()` yerine `useWatch`: watch her render'da yeni bir fonksiyon döndürdüğü için
  // React Compiler bu bileşeni memoize etmekten vazgeçiyor (oxlint react(incompatible-library)).
  const selectedRoles = useWatch({ control: createForm.control, name: 'roleNames' })
  const needsStudentFields = selectedRoles.includes(STUDENT_ROLE)
  const needsAdvisorFields = selectedRoles.includes(ADVISOR_ROLE)

  const invalidateUsers = () => queryClient.invalidateQueries({ queryKey: ['users'] })

  const createUserMutation = useMutation({
    mutationFn: async (values: CreateUserFormValues) => {
      // Y-67: profil alanları yalnızca ilgili rol seçiliyse gönderilir.
      await apiClient.post('/users', {
        email: values.email.trim(),
        password: values.password,
        firstName: values.firstName.trim() || null,
        lastName: values.lastName.trim() || null,
        roleNames: values.roleNames,
        studentNumber: values.roleNames.includes(STUDENT_ROLE) ? values.studentNumber.trim() : null,
        departmentId: values.departmentId || null,
        enrollmentYear: values.roleNames.includes(STUDENT_ROLE) ? Number(values.enrollmentYear) : null,
        title: values.roleNames.includes(ADVISOR_ROLE) ? values.title.trim() : null,
      })
    },
    onSuccess: () => {
      notify({ message: 'Kullanıcı oluşturuldu.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset(emptyCreateUserFormValues)
      invalidateUsers()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kullanıcı oluşturulamadı.'), severity: 'error' }),
  })

  const setUserRolesMutation = useMutation({
    mutationFn: async ({ userId, roleNames }: { userId: number; roleNames: string[] }) => {
      await apiClient.put(`/users/${userId}/roles`, { roleNames })
    },
    onSuccess: () => {
      notify({ message: 'Kullanıcı rolleri güncellendi.', severity: 'success' })
      setEditingUser(null)
      invalidateUsers()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Roller güncellenemedi.'), severity: 'error' }),
  })

  const lockoutMutation = useMutation({
    mutationFn: async ({ userId, locked }: { userId: number; locked: boolean }) => {
      await apiClient.put(`/users/${userId}/lockout`, { locked })
    },
    onSuccess: (_data, variables) => {
      notify({ message: variables.locked ? 'Kullanıcı pasife alındı.' : 'Kullanıcı aktifleştirildi.', severity: 'success' })
      invalidateUsers()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Durum güncellenemedi.'), severity: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (userId: number) => {
      await apiClient.delete(`/users/${userId}`)
    },
    onSuccess: () => {
      notify({ message: 'Kullanıcı silindi.', severity: 'success' })
      setDeleteTarget(null)
      invalidateUsers()
    },
    onError: (error) => {
      // A-57: bağlı kaydı olan kullanıcı 409 ile reddedilir — sebep API'den gelir (Y-35).
      notify({ message: extractErrorMessage(error, 'Kullanıcı silinemedi.'), severity: 'error' })
      setDeleteTarget(null)
    },
  })

  const openRoleDialog = (user: UserListItemDto) => {
    setEditingUser(user)
    setEditingRoleNames(new Set(user.roles))
  }

  const toggleRole = (name: string) => {
    setEditingRoleNames((prev) => {
      const next = new Set(prev)
      if (next.has(name)) {
        next.delete(name)
      } else {
        next.add(name)
      }
      return next
    })
  }

  const columns: GridColDef<UserListItemDto>[] = [
    { field: 'lastName', headerName: 'Ad Soyad', flex: 1, minWidth: 170, sortable: false, valueGetter: (_value, row) => displayName(row) },
    { field: 'email', headerName: 'E-posta', flex: 1, minWidth: 220 },
    {
      field: 'roles',
      headerName: 'Roller',
      flex: 1,
      minWidth: 200,
      sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5, py: 0.5 }}>
          {params.row.roles.map((name) => (
            <Chip key={name} size="small" label={name} />
          ))}
        </Stack>
      ),
    },
    {
      field: 'isLockedOut',
      headerName: 'Durum',
      width: 110,
      renderCell: (params) =>
        params.row.isLockedOut ? (
          <Chip size="small" label="Pasif" color="default" variant="outlined" />
        ) : (
          <Chip size="small" label="Aktif" color="success" />
        ),
    },
    {
      field: 'actions',
      headerName: '',
      width: 300,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        // Y-03/A-57: kendi hesabını kilitleyemez/silemez — API de reddeder, düğme boşuna durmasın.
        const isSelf = params.row.email === currentEmail

        return (
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" onClick={() => openRoleDialog(params.row)}>
              Roller
            </Button>
            <Button
              size="small"
              disabled={isSelf || lockoutMutation.isPending}
              onClick={() => lockoutMutation.mutate({ userId: params.row.id, locked: !params.row.isLockedOut })}
            >
              {params.row.isLockedOut ? 'Aktifleştir' : 'Pasife Al'}
            </Button>
            <Button size="small" color="error" disabled={isSelf} onClick={() => setDeleteTarget(params.row)}>
              Sil
            </Button>
          </Stack>
        )
      },
    },
  ]

  return (
    <>
      <PageHeader title="Kullanıcılar" description="Hesapları görüntüleyin, rollerini değiştirin, pasife alın veya silin." />

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
        <SearchField
          value={search}
          onChange={(value) => {
            setPaginationModel({ ...paginationModel, page: 0 })
            setSearch(value)
          }}
          placeholder="Ad soyad veya e-posta ara…"
        />
        <Button variant="contained" onClick={createDialog.openDialog}>
          Yeni Kullanıcı
        </Button>
      </Stack>

      <DataTable
        mobileHiddenFields={['roles', 'email']}
        rows={usersQuery.data?.items ?? []}
        columns={columns}
        getRowHeight={() => 'auto'}
        loading={usersQuery.isFetching}
        paginationMode="server"
        rowCount={usersQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Kullanıcı bulunamadı"
      />

      <Dialog open={editingUser !== null} onClose={() => setEditingUser(null)} fullWidth maxWidth="xs">
        <DialogTitle>{editingUser ? displayName(editingUser) : ''} — Roller</DialogTitle>
        <DialogContent>
          <Typography variant="caption" color="text.secondary">
            {editingUser?.email}
          </Typography>
          <FormGroup>
            {(rolesQuery.data?.items ?? []).map((role) => (
              <FormControlLabel
                key={role.id}
                control={<Checkbox checked={editingRoleNames.has(role.name)} onChange={() => toggleRole(role.name)} />}
                label={role.name}
              />
            ))}
          </FormGroup>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditingUser(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={setUserRolesMutation.isPending}
            onClick={() => editingUser && setUserRolesMutation.mutate({ userId: editingUser.id, roleNames: [...editingRoleNames] })}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          createForm.reset(emptyCreateUserFormValues)
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Yeni Kullanıcı</DialogTitle>
        <DialogContent>
          <Stack direction="row" spacing={1}>
            <Controller
              name="firstName"
              control={createForm.control}
              render={({ field, fieldState }) => (
                <TextField {...field} fullWidth margin="dense" label="Ad" error={!!fieldState.error} helperText={fieldState.error?.message} />
              )}
            />
            <Controller
              name="lastName"
              control={createForm.control}
              render={({ field, fieldState }) => (
                <TextField {...field} fullWidth margin="dense" label="Soyad" error={!!fieldState.error} helperText={fieldState.error?.message} />
              )}
            />
          </Stack>

          <Controller
            name="email"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth margin="dense" label="E-posta" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="password"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth margin="dense" type="password" label="Parola" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />

          <Controller
            name="roleNames"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <>
                <Typography variant="subtitle2" sx={{ mt: 2 }}>
                  Roller
                </Typography>
                <FormGroup row>
                  {(rolesQuery.data?.items ?? []).map((role) => (
                    <FormControlLabel
                      key={role.id}
                      control={
                        <Checkbox
                          checked={field.value.includes(role.name)}
                          onChange={() =>
                            field.onChange(
                              field.value.includes(role.name)
                                ? field.value.filter((name) => name !== role.name)
                                : [...field.value, role.name],
                            )
                          }
                        />
                      }
                      label={role.name}
                    />
                  ))}
                </FormGroup>
                {fieldState.error && (
                  <Typography variant="caption" color="error">
                    {fieldState.error.message}
                  </Typography>
                )}
              </>
            )}
          />

          {/* Y-67: profil gerektiren rol seçilmeden bu alanlar görünmez; seçilince zorunlu olur. */}
          {(needsStudentFields || needsAdvisorFields) && (
            <>
              <Typography variant="subtitle2" sx={{ mt: 2 }}>
                Profil bilgileri
              </Typography>
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
            </>
          )}

          {needsStudentFields && (
            <Stack direction="row" spacing={1}>
              <Controller
                name="studentNumber"
                control={createForm.control}
                render={({ field, fieldState }) => (
                  <TextField {...field} fullWidth margin="dense" label="Öğrenci No" error={!!fieldState.error} helperText={fieldState.error?.message} />
                )}
              />
              <Controller
                name="enrollmentYear"
                control={createForm.control}
                render={({ field, fieldState }) => (
                  <TextField {...field} fullWidth margin="dense" label="Kayıt Yılı" error={!!fieldState.error} helperText={fieldState.error?.message} />
                )}
              />
            </Stack>
          )}

          {needsAdvisorFields && (
            <Controller
              name="title"
              control={createForm.control}
              render={({ field, fieldState }) => (
                <TextField {...field} fullWidth margin="dense" label="Akademik Unvan" error={!!fieldState.error} helperText={fieldState.error?.message} />
              )}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              createForm.reset(emptyCreateUserFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createUserMutation.isPending}
            onClick={createForm.handleSubmit((values) => createUserMutation.mutate(values))}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Kullanıcıyı sil"
        description={
          deleteTarget
            ? `"${displayName(deleteTarget)}" (${deleteTarget.email}) kalıcı olarak silinecek. Kullanıcının üyeliği, etkinlik kaydı, başvurusu veya danışmanlığı varsa işlem reddedilir.`
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
