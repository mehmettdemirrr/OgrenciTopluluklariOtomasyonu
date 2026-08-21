import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef } from '@mui/x-data-grid'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { ClubRoleChip } from '../components/ui/StatusChip'
import type { ClubDetailDto, ClubMemberListItemDto, ClubRole, PagedResult } from '../api/types'
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

  return (
    <>
      <PageHeader title={clubQuery.data?.name ?? 'Topluluk'} description="Topluluk bilgileri, üyelik, etkinlik ve duyuru yönetimi." />

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
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  const updateMutation = useMutation({
    mutationFn: async () => {
      await apiClient.put(`/clubs/${clubId}`, { name: name.trim(), description: description.trim() || null })
    },
    onSuccess: () => {
      notify({ message: 'Topluluk güncellendi.', severity: 'success' })
      setEditDialogOpen(false)
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
    setName(club?.name ?? '')
    setDescription(club?.description ?? '')
    setEditDialogOpen(true)
  }

  if (!club) {
    return null
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

      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Topluluğu Düzenle</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label="Topluluk Adı" value={name} onChange={(event) => setName(event.target.value)} />
          <TextField
            fullWidth
            multiline
            minRows={2}
            margin="dense"
            label="Açıklama"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Vazgeç</Button>
          <Button variant="contained" disabled={name.trim() === '' || updateMutation.isPending} onClick={() => updateMutation.mutate()}>
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
