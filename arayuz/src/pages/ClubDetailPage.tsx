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
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
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
import { clubFormSchema, emptyClubFormValues, type ClubFormValues } from '../schemas/clubForm'
import type { AcademicStaffListItemDto, ClubDetailDto, ClubMemberListItemDto, ClubRole, PagedResult } from '../api/types'
import { ClubAnnouncementsTab } from './ClubDetailAnnouncementsTab'
import { ClubEventsTab } from './ClubDetailEventsTab'

const CLUB_ROLES: ClubRole[] = ['Member', 'Officer', 'President']

type TabKey = 'general' | 'members' | 'events' | 'announcements'

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
        {canViewEvents && <Tab label="Etkinlikler" value="events" />}
        {canViewAnnouncements && <Tab label="Duyurular" value="announcements" />}
      </Tabs>

      {tab === 'general' && <GeneralTab clubId={clubId} club={clubQuery.data} canManage={canManageClubs} />}
      {tab === 'members' && canViewMembers && <MembersTab clubId={clubId} />}
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

  const updateMutation = useMutation({
    mutationFn: async (values: ClubFormValues) => {
      await apiClient.put(`/clubs/${clubId}`, {
        name: values.name.trim(),
        description: values.description.trim() || null,
        // K-33: 0 seçilmediyse null gider ve mevcut danışman korunur.
        advisorId: values.advisorId || null,
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
    reset({ name: club?.name ?? '', description: club?.description ?? '', advisorId: club?.advisorId ?? 0 })
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
  const [removeTarget, setRemoveTarget] = useState<ClubMemberListItemDto | null>(null)

  const { paginationModel, setPaginationModel, query: membersQuery } = usePagedQuery({
    queryKey: ['club-members', clubId],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<ClubMemberListItemDto>>(`/clubs/${clubId}/members`, { params: { pageIndex, pageSize } })).data,
  })

  const setRoleMutation = useMutation({
    mutationFn: async () => {
      if (!roleTarget) return
      await apiClient.put(`/clubs/${clubId}/members/${roleTarget.membershipId}/role`, { clubRole: newRole })
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
  }

  const columns: GridColDef<ClubMemberListItemDto>[] = [
    { field: 'studentNumber', headerName: 'Öğrenci No', width: 160 },
    { field: 'clubRole', headerName: 'Rol', width: 140, renderCell: (params) => <ClubRoleChip role={params.row.clubRole} /> },
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
          <TextField select fullWidth margin="dense" label="Yeni Rol" value={newRole} onChange={(event) => setNewRole(event.target.value as ClubRole)}>
            {CLUB_ROLES.map((role) => (
              <MenuItem key={role} value={role}>
                {role === 'Member' ? 'Üye' : role === 'Officer' ? 'Yönetici' : 'Başkan'}
              </MenuItem>
            ))}
          </TextField>
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
