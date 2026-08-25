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
import type { PagedResult, PermissionCatalogItemDto, RoleListItemDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

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

/**
 * K-31 · Faz 29: eskiden "Yetki Matrisi" tek sayfa, iki sekmeydi. İki ekranın ortak hiçbir
 * durumu yoktu — sekme yalnızca iki ayrı işi tek URL'nin arkasına saklıyordu. Artık ayrı
 * rotalar: /authorization/roles ve /authorization/users.
 */
export function RolesPage() {
  useDocumentTitle('Roller ve İzinler')

  const permissionsQuery = useQuery({
    queryKey: ['permissions'],
    queryFn: async () => (await apiClient.get<PermissionCatalogItemDto[]>('/permissions')).data,
  })

  return (
    <>
      <PageHeader title="Roller ve İzinler" description="Rolleri oluşturun ve her rolün izinlerini yönetin." />
      <RolesTable permissionCatalog={permissionsQuery.data ?? []} />
    </>
  )
}

function RolesTable({ permissionCatalog }: { permissionCatalog: PermissionCatalogItemDto[] }) {
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
        mobileHiddenFields={['isSystemRole', 'permissions']}
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

