import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
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
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { PagedResult, PermissionCatalogItemDto, RoleListItemDto, UserListItemDto } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

type Snack = { message: string; severity: 'success' | 'error' } | null

export function AuthorizationPage() {
  const [tab, setTab] = useState(0)
  const [snackbar, setSnackbar] = useState<Snack>(null)

  const permissionsQuery = useQuery({
    queryKey: ['permissions'],
    queryFn: async () => (await apiClient.get<PermissionCatalogItemDto[]>('/permissions')).data,
  })

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        Yetki Matrisi
      </Typography>

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Roller / İzinler" />
        <Tab label="Kullanıcılar / Roller" />
      </Tabs>

      {tab === 0 && (
        <RolesTab permissionCatalog={permissionsQuery.data ?? []} onNotify={setSnackbar} />
      )}
      {tab === 1 && <UsersTab onNotify={setSnackbar} />}

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

function RolesTab({
  permissionCatalog,
  onNotify,
}: {
  permissionCatalog: PermissionCatalogItemDto[]
  onNotify: (snack: Snack) => void
}) {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [editingRole, setEditingRole] = useState<RoleListItemDto | null>(null)
  const [editingPermissions, setEditingPermissions] = useState<Set<string>>(new Set())
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [newRoleName, setNewRoleName] = useState('')

  const rolesQuery = useQuery({
    queryKey: ['roles', paginationModel.page, paginationModel.pageSize],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<RoleListItemDto>>('/roles', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
  })

  const createRoleMutation = useMutation({
    mutationFn: async (name: string) => {
      await apiClient.post('/roles', { name })
    },
    onSuccess: () => {
      onNotify({ message: 'Rol oluşturuldu.', severity: 'success' })
      setCreateDialogOpen(false)
      setNewRoleName('')
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Rol oluşturulamadı.'), severity: 'error' }),
  })

  const deleteRoleMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/roles/${id}`)
    },
    onSuccess: () => {
      onNotify({ message: 'Rol silindi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Rol silinemedi.'), severity: 'error' }),
  })

  const setPermissionsMutation = useMutation({
    mutationFn: async ({ roleId, permissions }: { roleId: number; permissions: string[] }) => {
      await apiClient.put(`/roles/${roleId}/permissions`, { permissions })
    },
    onSuccess: () => {
      onNotify({ message: 'Rol izinleri güncellendi.', severity: 'success' })
      setEditingRole(null)
      queryClient.invalidateQueries({ queryKey: ['roles'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'İzinler güncellenemedi.'), severity: 'error' }),
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
          <Button
            size="small"
            color="error"
            disabled={params.row.isSystemRole || deleteRoleMutation.isPending}
            onClick={() => deleteRoleMutation.mutate(params.row.id)}
          >
            Sil
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <Box>
      <Box sx={{ mb: 2 }}>
        <Button variant="contained" onClick={() => setCreateDialogOpen(true)}>
          Yeni Rol
        </Button>
      </Box>

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={rolesQuery.data?.items ?? []}
          columns={columns}
          getRowHeight={() => 'auto'}
          loading={rolesQuery.isFetching}
          paginationMode="server"
          rowCount={rolesQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
        />
      </Box>

      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Yeni Rol</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Rol Adı"
            value={newRoleName}
            onChange={(event) => setNewRoleName(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={newRoleName.trim() === '' || createRoleMutation.isPending}
            onClick={() => createRoleMutation.mutate(newRoleName.trim())}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editingRole !== null} onClose={() => setEditingRole(null)} fullWidth maxWidth="sm">
        <DialogTitle>{editingRole?.name} — İzinler</DialogTitle>
        <DialogContent>
          <FormGroup>
            {permissionCatalog.map((permission) => (
              <FormControlLabel
                key={permission.code}
                control={
                  <Checkbox checked={editingPermissions.has(permission.code)} onChange={() => togglePermission(permission.code)} />
                }
                label={`${permission.displayName} (${permission.code})`}
              />
            ))}
          </FormGroup>
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
    </Box>
  )
}

function UsersTab({ onNotify }: { onNotify: (snack: Snack) => void }) {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: DEFAULT_PAGE_SIZE })
  const [search, setSearch] = useState('')
  const [editingUser, setEditingUser] = useState<UserListItemDto | null>(null)
  const [editingRoleNames, setEditingRoleNames] = useState<Set<string>>(new Set())

  const usersQuery = useQuery({
    queryKey: ['users', paginationModel.page, paginationModel.pageSize, search],
    queryFn: async () => {
      const response = await apiClient.get<PagedResult<UserListItemDto>>('/users', {
        params: { pageIndex: paginationModel.page, pageSize: paginationModel.pageSize, search: search || undefined },
      })
      return response.data
    },
    placeholderData: (previousData) => previousData,
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
      onNotify({ message: 'Kullanıcı rolleri güncellendi.', severity: 'success' })
      setEditingUser(null)
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error) => onNotify({ message: extractErrorMessage(error, 'Roller güncellenemedi.'), severity: 'error' }),
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
    <Box>
      <TextField
        size="small"
        label="E-posta ara"
        value={search}
        onChange={(event) => {
          setPaginationModel((prev) => ({ ...prev, page: 0 }))
          setSearch(event.target.value)
        }}
        sx={{ mb: 2, width: 280 }}
      />

      <Box sx={{ height: 480 }}>
        <DataGrid
          rows={usersQuery.data?.items ?? []}
          columns={columns}
          getRowHeight={() => 'auto'}
          loading={usersQuery.isFetching}
          paginationMode="server"
          rowCount={usersQuery.data?.totalCount ?? 0}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
        />
      </Box>

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
    </Box>
  )
}
