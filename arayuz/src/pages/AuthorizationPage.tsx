import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { emptyNameFormValues, nameFormSchema, type NameFormValues } from '../schemas/referenceForm'
import type { PagedResult, PermissionCatalogItemDto, RoleListItemDto, UserListItemDto } from '../api/types'

function groupByCategory(catalog: PermissionCatalogItemDto[]): Map<string, PermissionCatalogItemDto[]> {
  const groups = new Map<string, PermissionCatalogItemDto[]>()
  for (const permission of catalog) {
    const category = permission.code.split('.')[0]
    const bucket = groups.get(category) ?? []
    bucket.push(permission)
    groups.set(category, bucket)
  }
  return groups
}

export function AuthorizationPage() {
  const [tab, setTab] = useState(0)

  const permissionsQuery = useQuery({
    queryKey: ['permissions'],
    queryFn: async () => (await apiClient.get<PermissionCatalogItemDto[]>('/permissions')).data,
  })

  return (
    <>
      <PageHeader title="Yetki Matrisi" description="Rolleri, izinlerini ve kullanıcı-rol atamalarını yönetin." />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Roller / İzinler" />
        <Tab label="Kullanıcılar / Roller" />
      </Tabs>

      {tab === 0 && <RolesTab permissionCatalog={permissionsQuery.data ?? []} />}
      {tab === 1 && <UsersTab />}
    </>
  )
}

function RolesTab({ permissionCatalog }: { permissionCatalog: PermissionCatalogItemDto[] }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [editingRole, setEditingRole] = useState<RoleListItemDto | null>(null)
  const [editingPermissions, setEditingPermissions] = useState<Set<string>>(new Set())
  const createDialog = useFormDialog()
  const createForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })
  const [deleteTarget, setDeleteTarget] = useState<RoleListItemDto | null>(null)

  const { paginationModel, setPaginationModel, query: rolesQuery } = usePagedQuery({
    queryKey: ['roles'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<RoleListItemDto>>('/roles', { params: { pageIndex, pageSize } })).data,
  })

  const permissionGroups = useMemo(() => groupByCategory(permissionCatalog), [permissionCatalog])

  const createRoleMutation = useMutation({
    mutationFn: async (values: NameFormValues) => {
      await apiClient.post('/roles', { name: values.name })
    },
    onSuccess: () => {
      notify({ message: 'Rol oluşturuldu.', severity: 'success' })
      createDialog.closeDialog()
      createForm.reset(emptyNameFormValues)
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Rol oluşturulamadı.'), severity: 'error' }),
  })

  const deleteRoleMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/roles/${id}`)
    },
    onSuccess: () => {
      notify({ message: 'Rol silindi.', severity: 'success' })
      setDeleteTarget(null)
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Rol silinemedi.'), severity: 'error' }),
  })

  const setPermissionsMutation = useMutation({
    mutationFn: async ({ roleId, permissions }: { roleId: number; permissions: string[] }) => {
      await apiClient.put(`/roles/${roleId}/permissions`, { permissions })
    },
    onSuccess: () => {
      notify({ message: 'Rol izinleri güncellendi.', severity: 'success' })
      setEditingRole(null)
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'İzinler güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (role: RoleListItemDto) => {
    setEditingRole(role)
    setEditingPermissions(new Set(role.permissions))
  }

  const togglePermission = (code: string) => {
    setEditingPermissions((prev) => {
      const next = new Set(prev)
      if (next.has(code)) {
        next.delete(code)
      } else {
        next.add(code)
      }
      return next
    })
  }

  const columns: GridColDef<RoleListItemDto>[] = [
    { field: 'name', headerName: 'Rol Adı', flex: 1, minWidth: 160 },
    {
      field: 'isSystemRole',
      headerName: 'Sistem Rolü',
      width: 130,
      valueFormatter: (value: boolean) => (value ? 'Evet' : 'Hayır'),
    },
    {
      field: 'permissions',
      headerName: 'İzinler',
      flex: 2,
      minWidth: 260,
      sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5, py: 0.5 }}>
          {params.row.permissions.map((code) => (
            <Chip key={code} size="small" label={code} />
          ))}
        </Stack>
      ),
    },
    {
      field: 'actions',
      headerName: '',
      width: 220,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button size="small" variant="outlined" onClick={() => openEditDialog(params.row)}>
            İzinleri Düzenle
          </Button>
          <Button size="small" color="error" disabled={params.row.isSystemRole} onClick={() => setDeleteTarget(params.row)}>
            Sil
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <>
      <Box sx={{ mb: 2 }}>
        <Button variant="contained" onClick={createDialog.openDialog}>
          Yeni Rol
        </Button>
      </Box>

      <DataTable
        rows={rolesQuery.data?.items ?? []}
        columns={columns}
        getRowHeight={() => 'auto'}
        loading={rolesQuery.isFetching}
        paginationMode="server"
        rowCount={rolesQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Rol bulunamadı"
      />

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          createForm.reset(emptyNameFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Rol</DialogTitle>
        <DialogContent>
          <Controller
            name="name"
            control={createForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} autoFocus fullWidth margin="dense" label="Rol Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              createDialog.closeDialog()
              createForm.reset(emptyNameFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createForm.formState.isSubmitting || createRoleMutation.isPending}
            onClick={createForm.handleSubmit((values) => createRoleMutation.mutate(values))}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editingRole !== null} onClose={() => setEditingRole(null)} fullWidth maxWidth="sm">
        <DialogTitle>{editingRole?.name} — İzinler</DialogTitle>
        <DialogContent>
          {Array.from(permissionGroups.entries()).map(([category, permissions]) => (
            <Box key={category} sx={{ mb: 2 }}>
              <Typography variant="overline" color="text.secondary">
                {category}
              </Typography>
              <FormGroup>
                {permissions.map((permission) => (
                  <FormControlLabel
                    key={permission.code}
                    control={
                      <Checkbox checked={editingPermissions.has(permission.code)} onChange={() => togglePermission(permission.code)} />
                    }
                    label={`${permission.displayName} (${permission.code})`}
                  />
                ))}
              </FormGroup>
            </Box>
          ))}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditingRole(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={setPermissionsMutation.isPending}
            onClick={() =>
              editingRole &&
              setPermissionsMutation.mutate({ roleId: editingRole.id, permissions: Array.from(editingPermissions) })
            }
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Rolü sil"
        description={deleteTarget ? `"${deleteTarget.name}" rolünü silmek istediğinize emin misiniz?` : undefined}
        confirmLabel="Sil"
        destructive
        loading={deleteRoleMutation.isPending}
        onConfirm={() => deleteTarget && deleteRoleMutation.mutate(deleteTarget.id)}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  )
}

function UsersTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [search, setSearch] = useState('')
  const [editingUser, setEditingUser] = useState<UserListItemDto | null>(null)
  const [editingRoleNames, setEditingRoleNames] = useState<Set<string>>(new Set())

  const { paginationModel, setPaginationModel, query: usersQuery } = usePagedQuery({
    queryKey: ['users', search],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<UserListItemDto>>('/users', { params: { pageIndex, pageSize, search: search || undefined } })).data,
  })

  // Rol adı listesi için tam liste gerekiyor — matris ekranında zaten yüklü olan rolleri yeniden kullanır.
  const rolesQuery = useQuery({
    queryKey: ['roles', 0, 200],
    queryFn: async () => (await apiClient.get<PagedResult<RoleListItemDto>>('/roles', { params: { pageIndex: 0, pageSize: 200 } })).data,
  })

  const setUserRolesMutation = useMutation({
    mutationFn: async ({ userId, roleNames }: { userId: number; roleNames: string[] }) => {
      await apiClient.put(`/users/${userId}/roles`, { roleNames })
    },
    onSuccess: () => {
      notify({ message: 'Kullanıcı rolleri güncellendi.', severity: 'success' })
      setEditingUser(null)
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Roller güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = (user: UserListItemDto) => {
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
    { field: 'email', headerName: 'E-posta', flex: 1, minWidth: 220 },
    {
      field: 'roles',
      headerName: 'Roller',
      flex: 1,
      minWidth: 220,
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
      field: 'actions',
      headerName: '',
      width: 160,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" variant="outlined" onClick={() => openEditDialog(params.row)}>
          Rolleri Düzenle
        </Button>
      ),
    },
  ]

  return (
    <>
      <TextField
        size="small"
        label="E-posta ara"
        value={search}
        onChange={(event) => {
          setPaginationModel({ ...paginationModel, page: 0 })
          setSearch(event.target.value)
        }}
        sx={{ mb: 2, width: 280 }}
      />

      <DataTable
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
        <DialogTitle>{editingUser?.email} — Roller</DialogTitle>
        <DialogContent>
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
            onClick={() =>
              editingUser && setUserRolesMutation.mutate({ userId: editingUser.id, roleNames: Array.from(editingRoleNames) })
            }
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
