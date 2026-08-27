import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Alert,
  Checkbox,
  FormControl,
  FormControlLabel,
  FormGroup,
  FormLabel,
  IconButton,
  MenuItem,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import type { GridColDef } from '@mui/x-data-grid'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { useState } from 'react'
import { Controller, useForm, type Control } from 'react-hook-form'
import { useParams } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { SectionCard } from '../components/ui/SectionCard'
import { ClubRoleChip } from '../components/ui/StatusChip'
import { clubFormSchema, emptyClubFormValues, toCategoryPayload, type ClubFormValues } from '../schemas/clubForm'
import {
  clubRoleDefinitionFormSchema,
  emptyClubRoleDefinitionFormValues,
  type ClubRoleDefinitionFormValues,
} from '../schemas/clubRoleDefinitionForm'
import { CLUB_CAPABILITIES } from '../api/types'
import type {
  AcademicStaffListItemDto,
  ClubCategoryListItemDto,
  ClubDetailDto,
  ClubMemberListItemDto,
  ClubRole,
  ClubRoleDefinitionDto,
  PagedResult,
} from '../api/types'
import { ClubAnnouncementsTab } from './ClubDetailAnnouncementsTab'
import { ClubEventsTab } from './ClubDetailEventsTab'

const CLUB_ROLES: ClubRole[] = ['Member', 'Officer', 'President']

type TabKey = 'general' | 'members' | 'roles' | 'events' | 'announcements'

/** K-36: yetki seviyesinin görünen adı — üç yerde tekrarlanmasın diye tek yerde.  */
function clubRoleLabel(role: ClubRole): string {
  return role === 'Member' ? 'Üye' : role === 'Officer' ? 'Yönetici' : 'Başkan'
}

export function ClubDetailPage() {
  const { id } = useParams<{ id: string }>()
  const clubId = Number(id)
  const [tab, setTab] = useState<TabKey>('general')
  const { hasPermission } = useAuth()
  const canManageClubs = hasPermission(Permissions.ClubsWrite)
  const canViewMembers = hasPermission(Permissions.MembershipsRead)
  const canViewEvents = hasPermission(Permissions.EventsRead)
  const canViewAnnouncements = hasPermission(Permissions.ClubsRead)

  const clubQuery = useQuery({
    queryKey: ['clubs', clubId],
    queryFn: async () => (await apiClient.get<ClubDetailDto>(`/clubs/${clubId}`)).data,
  })

  useDocumentTitle(clubQuery.data?.name)

  const managesAllClubs = hasPermission(Permissions.ClubsManageAll)

  return (
    <>
      <PageHeader title={clubQuery.data?.name ?? 'Topluluk'} description="Topluluk bilgileri, üyelik, etkinlik ve duyuru yönetimi." />

      {/* §25.4: hangi yetkiyle işlem yapıldığı belirsiz kalmasın — danışman olmadan yönetiliyor. */}
      {managesAllClubs && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Bu topluluğu <strong>yönetici yetkisiyle</strong> görüntülüyorsunuz; danışmanı veya başkanı olmasanız da
          etkinlik, duyuru, üye rolü ve logo işlemlerini yapabilirsiniz.
        </Alert>
      )}

      <Tabs value={tab} onChange={(_, value: TabKey) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Genel" value="general" />
        {canViewMembers && <Tab label="Üyeler" value="members" />}
        {/* Y-35: sekmeyi gizlemek yetki DEĞİL, kolaylıktır — uç kendi 403'ünü döner. */}
        {canViewMembers && <Tab label="Roller" value="roles" />}
        {canViewEvents && <Tab label="Etkinlikler" value="events" />}
        {canViewAnnouncements && <Tab label="Duyurular" value="announcements" />}
      </Tabs>

      {tab === 'general' && <GeneralTab clubId={clubId} club={clubQuery.data} canManage={canManageClubs} />}
      {tab === 'members' && canViewMembers && <MembersTab clubId={clubId} />}
      {tab === 'roles' && canViewMembers && <RoleDefinitionsTab clubId={clubId} />}
      {tab === 'events' && canViewEvents && <ClubEventsTab clubId={clubId} />}
      {tab === 'announcements' && canViewAnnouncements && <ClubAnnouncementsTab clubId={clubId} />}
    </>
  )
}

function GeneralTab({ clubId, club, canManage }: { clubId: number; club: ClubDetailDto | undefined; canManage: boolean }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const editDialog = useFormDialog()
  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting: isFormSubmitting },
  } = useForm<ClubFormValues>({
    resolver: zodResolver(clubFormSchema),
    defaultValues: emptyClubFormValues,
  })

  const categoriesQuery = useQuery({
    queryKey: ['club-categories'],
    queryFn: async () =>
      (await apiClient.get<PagedResult<ClubCategoryListItemDto>>('/club-categories', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })

  const updateMutation = useMutation({
    mutationFn: async (values: ClubFormValues) => {
      await apiClient.put(`/clubs/${clubId}`, {
        name: values.name.trim(),
        description: values.description.trim() || null,
        // K-33: 0 seçilmediyse null gider ve mevcut danışman korunur.
        advisorId: values.advisorId || null,
        // A-60: burada null "kategorisiz yap" demek — AdvisorId'den farklı semantik.
        clubCategoryId: toCategoryPayload(values.clubCategoryId),
      })
    },
    onSuccess: () => {
      notify({ message: 'Topluluk güncellendi.', severity: 'success' })
      editDialog.closeDialog()
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Topluluk güncellenemedi.'), severity: 'error' }),
  })

  const statusMutation = useMutation({
    mutationFn: async (isActive: boolean) => {
      await apiClient.put(`/clubs/${clubId}/status`, { isActive })
    },
    onSuccess: (_data, isActive) => {
      notify({ message: isActive ? 'Topluluk aktifleştirildi.' : 'Topluluk pasife alındı.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Durum güncellenemedi.'), severity: 'error' }),
  })

  const openEditDialog = () => {
    reset({
      name: club?.name ?? '',
      description: club?.description ?? '',
      advisorId: club?.advisorId ?? 0,
      clubCategoryId: club?.clubCategoryId ?? 0,
    })
    editDialog.openDialog()
  }

  // §23.2: bomboş ekran yerine iskelet — üst bileşen `club` gelene kadar undefined geçer.
  if (!club) {
    return <Skeleton variant="rounded" height={180} />
  }

  return (
    <SectionCard
      action={
        canManage && (
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" onClick={openEditDialog}>
              Düzenle
            </Button>
            <Button
              size="small"
              color={club.isActive ? 'error' : 'success'}
              disabled={statusMutation.isPending}
              onClick={() => statusMutation.mutate(!club.isActive)}
            >
              {club.isActive ? 'Pasife Al' : 'Aktifleştir'}
            </Button>
          </Stack>
        )
      }
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Chip size="small" label={club.isActive ? 'Aktif' : 'Pasif'} color={club.isActive ? 'success' : 'default'} />
          {club.clubCategoryName && <Chip size="small" variant="outlined" label={club.clubCategoryName} />}
          <Typography variant="caption" color="text.secondary">
            Oluşturulma: {new Date(club.createdAtUtc).toLocaleDateString('tr-TR')}
          </Typography>
        </Stack>
        <Typography variant="body2">{club.description || 'Açıklama eklenmemiş.'}</Typography>
      </Stack>

      <Dialog open={editDialog.open} onClose={editDialog.closeDialog} fullWidth maxWidth="xs">
        <DialogTitle>Topluluğu Düzenle</DialogTitle>
        <DialogContent>
          <Controller
            name="name"
            control={control}
            render={({ field, fieldState }) => (
              <TextField {...field} autoFocus fullWidth margin="dense" label="Topluluk Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="description"
            control={control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={2} margin="dense" label="Açıklama" />}
          />

          {/* K-33: danışman değişimi. Eski danışman bu kulüpteki tüm yetkisini anında kaybeder. */}
          <Controller
            name="advisorId"
            control={control}
            render={({ field, fieldState }) => (
              <RemoteSelect<AcademicStaffListItemDto>
                label="Danışman"
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? 0)}
                queryKey={['academic-staff', 'club-advisor']}
                enabled={editDialog.open}
                fetchOptions={async (term) =>
                  (await apiClient.get<PagedResult<AcademicStaffListItemDto>>('/academic-staff', {
                    params: { pageIndex: 0, pageSize: 20, search: term || undefined },
                  })).data.items
                }
                getOptionId={(staff) => staff.id}
                getOptionLabel={(staff) =>
                  [staff.title, [staff.firstName, staff.lastName].filter(Boolean).join(' ') || staff.email].join(' ')
                }
                error={!!fieldState.error}
                helperText={fieldState.error?.message ?? 'Boş bırakılırsa mevcut danışman korunur.'}
              />
            )}
          />

          <Controller
            name="clubCategoryId"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                fullWidth
                margin="dense"
                label="Kategori (isteğe bağlı)"
                onChange={(event) => field.onChange(Number(event.target.value))}
              >
                <MenuItem value={0}>— Kategorisiz —</MenuItem>
                {(categoriesQuery.data?.items ?? []).map((category) => (
                  <MenuItem key={category.id} value={category.id}>
                    {category.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={editDialog.closeDialog}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={isFormSubmitting || updateMutation.isPending}
            onClick={handleSubmit((values) => updateMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </SectionCard>
  )
}

function MembersTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canManageMembers = hasPermission(Permissions.MembershipsWrite)

  const [roleTarget, setRoleTarget] = useState<ClubMemberListItemDto | null>(null)
  const [newRole, setNewRole] = useState<ClubRole>('Member')
  // K-36: 0 = unvansız atama (bugünkü davranış); >0 ise seviye tanımdan gelir.
  const [newDefinitionId, setNewDefinitionId] = useState<number>(0)
  const [removeTarget, setRemoveTarget] = useState<ClubMemberListItemDto | null>(null)

  const { paginationModel, setPaginationModel, query: membersQuery } = usePagedQuery({
    queryKey: ['club-members', clubId],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<ClubMemberListItemDto>>(`/clubs/${clubId}/members`, { params: { pageIndex, pageSize } })).data,
  })

  const definitionsQuery = useQuery({
    queryKey: ['club-role-definitions', clubId],
    queryFn: async () => (await apiClient.get<ClubRoleDefinitionDto[]>(`/clubs/${clubId}/role-definitions`)).data,
  })

  const selectedDefinition = (definitionsQuery.data ?? []).find((d) => d.id === newDefinitionId)

  const setRoleMutation = useMutation({
    mutationFn: async () => {
      if (!roleTarget) return
      // Y-22: unvan seçildiyse clubRole GÖNDERİLMEZ — sunucu tanımdan okur.
      const payload = newDefinitionId > 0 ? { clubRoleDefinitionId: newDefinitionId } : { clubRole: newRole }
      await apiClient.put(`/clubs/${clubId}/members/${roleTarget.membershipId}/role`, payload)
    },
    onSuccess: () => {
      notify({ message: 'Üye rolü güncellendi.', severity: 'success' })
      setRoleTarget(null)
      queryClient.invalidateQueries({ queryKey: ['club-members', clubId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Rol güncellenemedi.'), severity: 'error' }),
  })

  const removeMutation = useMutation({
    mutationFn: async () => {
      if (!removeTarget) return
      await apiClient.delete(`/clubs/${clubId}/members/${removeTarget.membershipId}`)
    },
    onSuccess: () => {
      notify({ message: 'Üye topluluktan çıkarıldı.', severity: 'success' })
      setRemoveTarget(null)
      queryClient.invalidateQueries({ queryKey: ['club-members', clubId] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Üye çıkarılamadı.'), severity: 'error' }),
  })

  const openRoleDialog = (member: ClubMemberListItemDto) => {
    setRoleTarget(member)
    setNewRole(member.clubRole)
    setNewDefinitionId(member.clubRoleDefinitionId ?? 0)
  }

  const columns: GridColDef<ClubMemberListItemDto>[] = [
    { field: 'studentNumber', headerName: 'Öğrenci No', width: 160 },
    {
      field: 'clubRole',
      headerName: 'Rol',
      width: 220,
      // K-36/A-61: unvan ve yetki seviyesi BİRLİKTE gösterilir — "Sayman" rozetinin yanındaki
      // "Yönetici" rozeti, o unvanın hangi yetkiyi verdiğini görünür kılar.
      renderCell: (params) =>
        params.row.clubRoleName ? (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <Chip size="small" label={params.row.clubRoleName} />
            <ClubRoleChip role={params.row.clubRole} />
          </Stack>
        ) : (
          <ClubRoleChip role={params.row.clubRole} />
        ),
    },
    {
      field: 'joinedAtUtc',
      headerName: 'Katılım Tarihi',
      width: 160,
      valueFormatter: (value: string) => new Date(value).toLocaleDateString('tr-TR'),
    },
    ...(canManageMembers
      ? [
          {
            field: 'actions',
            headerName: '',
            width: 220,
            sortable: false,
            filterable: false,
            renderCell: (params: { row: ClubMemberListItemDto }) => (
              <Stack direction="row" spacing={1}>
                <Button size="small" variant="outlined" onClick={() => openRoleDialog(params.row)}>
                  Rol Değiştir
                </Button>
                <Button size="small" color="error" onClick={() => setRemoveTarget(params.row)}>
                  Çıkar
                </Button>
              </Stack>
            ),
          } satisfies GridColDef<ClubMemberListItemDto>,
        ]
      : []),
  ]

  return (
    <Box>
      <DataTable
        mobileHiddenFields={['joinedAtUtc']}
        rows={membersQuery.data?.items ?? []}
        columns={columns}
        getRowId={(row) => row.membershipId}
        loading={membersQuery.isFetching}
        paginationMode="server"
        rowCount={membersQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Bu toplulukta henüz üye yok"
      />

      <Dialog open={roleTarget !== null} onClose={() => setRoleTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>{roleTarget?.studentNumber} — Rol Değiştir</DialogTitle>
        <DialogContent>
          <TextField
            select
            fullWidth
            margin="dense"
            label="Unvan"
            value={newDefinitionId}
            onChange={(event) => setNewDefinitionId(Number(event.target.value))}
          >
            <MenuItem value={0}>— Unvansız (yalnızca yetki seviyesi) —</MenuItem>
            {(definitionsQuery.data ?? []).map((definition) => (
              <MenuItem key={definition.id} value={definition.id}>
                {definition.name}
              </MenuItem>
            ))}
          </TextField>

          {newDefinitionId > 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
              Bu unvan <strong>{clubRoleLabel(selectedDefinition?.clubRole ?? 'Member')}</strong> yetki seviyesini verir.
            </Typography>
          ) : (
            <TextField
              select
              fullWidth
              margin="dense"
              label="Yetki Seviyesi"
              value={newRole}
              onChange={(event) => setNewRole(event.target.value as ClubRole)}
            >
              {CLUB_ROLES.map((role) => (
                <MenuItem key={role} value={role}>
                  {clubRoleLabel(role)}
                </MenuItem>
              ))}
            </TextField>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRoleTarget(null)}>Vazgeç</Button>
          <Button variant="contained" disabled={setRoleMutation.isPending} onClick={() => setRoleMutation.mutate()}>
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={removeTarget !== null}
        title="Üyeyi topluluktan çıkar"
        description={removeTarget ? `${removeTarget.studentNumber} numaralı öğrenciyi bu topluluktan çıkarmak istediğinize emin misiniz?` : undefined}
        confirmLabel="Çıkar"
        destructive
        loading={removeMutation.isPending}
        onConfirm={() => removeMutation.mutate()}
        onCancel={() => setRemoveTarget(null)}
      />
    </Box>
  )
}

/**
 * docs/MIMARI.md · K-36/A-61: kulübün rol UNVANLARI. Her unvan bir yetki seviyesine bağlıdır —
 * seviye burada açıkça gösterilir, çünkü "Sayman" unvanını veren kişi aynı zamanda Yönetici
 * yetkisi verdiğini GÖRMEDEN vermemeli.
 */
function RoleDefinitionsTab({ clubId }: { clubId: number }) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(Permissions.MembershipsWrite)

  const dialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<ClubRoleDefinitionDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<ClubRoleDefinitionDto | null>(null)

  const createForm = useForm<ClubRoleDefinitionFormValues>({
    resolver: zodResolver(clubRoleDefinitionFormSchema),
    defaultValues: emptyClubRoleDefinitionFormValues,
  })
  const editForm = useForm<ClubRoleDefinitionFormValues>({
    resolver: zodResolver(clubRoleDefinitionFormSchema),
    defaultValues: emptyClubRoleDefinitionFormValues,
  })

  const definitionsQuery = useQuery({
    queryKey: ['club-role-definitions', clubId],
    queryFn: async () => (await apiClient.get<ClubRoleDefinitionDto[]>(`/clubs/${clubId}/role-definitions`)).data,
  })

  // O-27: seviye değişikliği üyeliklere yayılıyor — üye listesi de tazelenmeli.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['club-role-definitions', clubId] })
    queryClient.invalidateQueries({ queryKey: ['club-members', clubId] })
  }

  const createMutation = useMutation({
    mutationFn: async (values: ClubRoleDefinitionFormValues) => {
      await apiClient.post(`/clubs/${clubId}/role-definitions`, values)
    },
    onSuccess: () => {
      notify({ message: 'Rol tanımı eklendi.', severity: 'success' })
      dialog.closeDialog()
      createForm.reset(emptyClubRoleDefinitionFormValues)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Rol tanımı eklenemedi.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: ClubRoleDefinitionFormValues) => {
      if (!editTarget) return
      await apiClient.put(`/clubs/${clubId}/role-definitions/${editTarget.id}`, values)
    },
    onSuccess: () => {
      notify({ message: 'Rol tanımı güncellendi.', severity: 'success' })
      setEditTarget(null)
      invalidate()
    },
    // A-39: ikinci başkan üretecekse API 409 döner; mesajı olduğu gibi gösteriyoruz (Y-35).
    onError: (error) => notify({ message: extractErrorMessage(error, 'Rol tanımı güncellenemedi.'), severity: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/clubs/${clubId}/role-definitions/${id}`)
    },
    onSuccess: () => {
      notify({ message: 'Rol tanımı silindi.', severity: 'success' })
      setDeleteTarget(null)
      invalidate()
    },
    onError: (error) => {
      // A-61: kullanımdaki unvan 409 döner.
      notify({ message: extractErrorMessage(error, 'Rol tanımı silinemedi.'), severity: 'error' })
      setDeleteTarget(null)
    },
  })

  const columns: GridColDef<ClubRoleDefinitionDto>[] = [
    { field: 'displayOrder', headerName: 'Sıra', width: 80 },
    { field: 'name', headerName: 'Unvan', flex: 1, minWidth: 200 },
    {
      field: 'clubRole',
      headerName: 'Makam',
      width: 130,
      renderCell: (params) => <ClubRoleChip role={params.row.clubRole} />,
    },
    {
      field: 'capabilities',
      headerName: 'Yetkiler',
      flex: 1,
      minWidth: 240,
      sortable: false,
      renderCell: (params) => {
        const granted = CLUB_CAPABILITIES.filter((c) => (params.row.capabilities & c.value) === c.value)
        return granted.length === 0 ? (
          <Typography variant="caption" color="text.secondary">
            Yetki yok
          </Typography>
        ) : (
          <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 0.5, py: 0.5 }}>
            {granted.map((c) => (
              <Chip key={c.value} size="small" variant="outlined" label={c.label} />
            ))}
          </Stack>
        )
      },
    },
    ...(canManage
      ? [
          {
            field: 'actions',
            headerName: '',
            width: 120,
            sortable: false,
            filterable: false,
            renderCell: (params: { row: ClubRoleDefinitionDto }) => (
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <IconButton
                  size="small"
                  onClick={() => {
                    setEditTarget(params.row)
                    editForm.reset({
                      name: params.row.name,
                      clubRole: params.row.clubRole,
                      capabilities: params.row.capabilities,
                      displayOrder: params.row.displayOrder,
                    })
                  }}
                >
                  <EditOutlinedIcon fontSize="small" />
                </IconButton>
                <IconButton size="small" color="error" onClick={() => setDeleteTarget(params.row)}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Stack>
            ),
          } satisfies GridColDef<ClubRoleDefinitionDto>,
        ]
      : []),
  ]

  return (
    <Box>
      {canManage && (
        <Box sx={{ mb: 2 }}>
          <Button variant="contained" onClick={dialog.openDialog}>
            Yeni Unvan
          </Button>
        </Box>
      )}

      <DataTable
        rows={definitionsQuery.data ?? []}
        columns={columns}
        getRowHeight={() => 'auto'}
        loading={definitionsQuery.isFetching}
        emptyTitle="Bu toplulukta henüz rol tanımı yok"
        emptyDescription="Sayman, Sekreter gibi unvanları tanımlayıp üyelere atayabilirsiniz."
      />

      <Dialog
        open={dialog.open}
        onClose={() => {
          dialog.closeDialog()
          createForm.reset(emptyClubRoleDefinitionFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Unvan</DialogTitle>
        <DialogContent>
          <ClubRoleDefinitionFormFields control={createForm.control} />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              dialog.closeDialog()
              createForm.reset(emptyClubRoleDefinitionFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createMutation.isPending}
            onClick={createForm.handleSubmit((values) => createMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Unvanı Düzenle</DialogTitle>
        <DialogContent>
          <ClubRoleDefinitionFormFields control={editForm.control} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={updateMutation.isPending}
            onClick={editForm.handleSubmit((values) => updateMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Unvanı sil"
        description={
          deleteTarget
            ? `"${deleteTarget.name}" unvanını silmek istediğinize emin misiniz? Bu unvanı taşıyan üye varsa silinemez.`
            : undefined
        }
        confirmLabel="Sil"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  )
}

function ClubRoleDefinitionFormFields({ control }: { control: Control<ClubRoleDefinitionFormValues> }) {
  return (
    <>
      <Controller
        name="name"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            autoFocus
            fullWidth
            margin="dense"
            label="Unvan adı"
            placeholder="Sosyal Medya Sorumlusu"
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
          />
        )}
      />
      <Controller
        name="clubRole"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            select
            fullWidth
            margin="dense"
            label="Makam"
            helperText="Yalnızca başkan tekilliği ve dönem devri için kullanılır; yetki aşağıdan seçilir."
          >
            {CLUB_ROLES.map((role) => (
              <MenuItem key={role} value={role}>
                {clubRoleLabel(role)}
              </MenuItem>
            ))}
          </TextField>
        )}
      />

      {/* A-68: yetki artık makamdan gelmiyor — her kutucuk bir kulüp içi işlem. */}
      <Controller
        name="capabilities"
        control={control}
        render={({ field }) => (
          <FormControl component="fieldset" sx={{ mt: 2, display: 'block' }}>
            <FormLabel component="legend">Bu unvan neler yapabilir?</FormLabel>
            <FormGroup>
              {CLUB_CAPABILITIES.map((capability) => (
                <FormControlLabel
                  key={capability.value}
                  sx={{ alignItems: 'flex-start', mt: 1 }}
                  control={
                    <Checkbox
                      sx={{ pt: 0 }}
                      checked={(field.value & capability.value) === capability.value}
                      onChange={(event) =>
                        field.onChange(
                          event.target.checked
                            ? field.value | capability.value
                            : field.value & ~capability.value,
                        )
                      }
                    />
                  }
                  label={
                    <Box>
                      <Typography variant="body2">{capability.label}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {capability.description}
                      </Typography>
                    </Box>
                  }
                />
              ))}
            </FormGroup>
            {/* Y-75: kullanıcıya sınırı söyle — kutucuk, kişinin sistemdeki rolünden fazlasını vermez. */}
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
              Bu yetkiler yalnızca bu toplulukta geçerlidir ve kişinin sistemdeki rolünün izin
              verdiğinden fazlasını veremez.
            </Typography>
          </FormControl>
        )}
      />

      <Controller
        name="displayOrder"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            type="number"
            fullWidth
            margin="dense"
            label="Sıra"
            onChange={(event) => field.onChange(Number(event.target.value))}
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
          />
        )}
      />
    </>
  )
}
