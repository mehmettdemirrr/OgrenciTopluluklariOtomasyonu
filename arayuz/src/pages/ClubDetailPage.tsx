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
  Paper,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import type { GridColDef } from '@mui/x-data-grid'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import PhoneOutlinedIcon from '@mui/icons-material/PhoneOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { useState } from 'react'
import { Controller, useFieldArray, useForm, type Control } from 'react-hook-form'
import { useParams } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { ClubProfileHeader } from '../components/clubs/ClubProfileHeader'
import { useFormDialog } from '../hooks/useFormDialog'
import { usePagedQuery } from '../hooks/usePagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DataTable } from '../components/ui/DataTable'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { SOCIAL_PLATFORMS, SOCIAL_PLATFORM_LABELS, SocialLinkIcons } from '../components/ui/SocialLinks'
import { ClubRoleChip } from '../components/ui/StatusChip'
import { clubFormSchema, emptyClubFormValues, type ClubFormValues } from '../schemas/clubForm'
import {
  clubContactFormSchema,
  emptyClubContactFormValues,
  toClubContactPayload,
  type ClubContactFormValues,
} from '../schemas/clubContactForm'
import {
  clubRoleDefinitionFormSchema,
  emptyClubRoleDefinitionFormValues,
  type ClubRoleDefinitionFormValues,
} from '../schemas/clubRoleDefinitionForm'
import { CLUB_CAPABILITIES, ClubCapability } from '../api/types'
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
  // A-75'in değiştirmediği kapı: Club.Name/Description hâlâ yalnızca Admin'in (clubs.write) elinde.
  const canManageClubs = hasPermission(Permissions.ClubsWrite)
  // docs/MIMARI.md · A-74: iletişim/sosyal bağlantı kapısı Club.Name/Description'dan farklı — bkz. AnnouncementsManage kapasitesi.
  const canManageContact = hasPermission(Permissions.AnnouncementsWrite)
  const canViewAnnouncements = hasPermission(Permissions.ClubsRead)

  const clubQuery = useQuery({
    queryKey: ['clubs', clubId],
    queryFn: async () => (await apiClient.get<ClubDetailDto>(`/clubs/${clubId}`)).data,
  })

  useDocumentTitle(clubQuery.data?.name)

  const managesAllClubs = hasPermission(Permissions.ClubsManageAll)

  // docs/MIMARI.md · A-75: sekme görünürlüğü global izinle değil, BU kulüpteki kapasiteyle belirlenir —
  // başka bir kulüpte yetkili olan öğrenci, üyesi olmadığı bu kulübün Üyeler/Roller sekmesini görmemeli.
  const capabilities = clubQuery.data?.myCapabilities ?? 0
  const canViewMembers = (capabilities & ClubCapability.MembersView) !== 0
  const canViewRoles = (capabilities & ClubCapability.MembersManage) !== 0
  const canManageEvents = (capabilities & ClubCapability.EventsManage) !== 0
  // K-45: etkinlik sekmesi herkese açıktır — yetkisiz kullanıcı yayınlanmış etkinlikleri görür.
  const canViewEvents = true

  return (
    <>
      <PageHeader
        title={clubQuery.data?.name ?? 'Topluluk'}
        description="Topluluk bilgileri, üyelik, etkinlik ve duyuru yönetimi."
        backTo="/clubs"
      />

      {/* §25.4: hangi yetkiyle işlem yapıldığı belirsiz kalmasın — danışman olmadan yönetiliyor. */}
      {managesAllClubs && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Bu topluluğu <strong>yönetici yetkisiyle</strong> görüntülüyorsunuz; danışmanı veya başkanı olmasanız da
          etkinlik, duyuru, üye rolü ve logo işlemlerini yapabilirsiniz.
        </Alert>
      )}

      <Paper variant="outlined" sx={{ mb: 3, px: { xs: 1, md: 1.5 }, borderRadius: 3 }}>
        <Tabs value={tab} onChange={(_, value: TabKey) => setTab(value)} variant="scrollable" allowScrollButtonsMobile>
          <Tab label="Genel" value="general" />
          {canViewMembers && <Tab label="Üyeler" value="members" />}
          {/* Y-35: sekmeyi gizlemek yetki DEĞİL, kolaylıktır — uç kendi 403'ünü döner. */}
          {canViewRoles && <Tab label="Roller" value="roles" />}
          {canViewEvents && <Tab label="Etkinlikler" value="events" />}
          {canViewAnnouncements && <Tab label="Duyurular" value="announcements" />}
        </Tabs>
      </Paper>

      {tab === 'general' && (
        <GeneralTab clubId={clubId} club={clubQuery.data} canManage={canManageClubs} canManageContact={canManageContact} />
      )}
      {tab === 'members' && canViewMembers && <MembersTab clubId={clubId} />}
      {tab === 'roles' && canViewRoles && <RoleDefinitionsTab clubId={clubId} />}
      {tab === 'events' && canViewEvents && <ClubEventsTab clubId={clubId} canManage={canManageEvents} />}
      {tab === 'announcements' && canViewAnnouncements && <ClubAnnouncementsTab clubId={clubId} />}
    </>
  )
}

function GeneralTab({
  clubId,
  club,
  canManage,
  canManageContact,
}: {
  clubId: number
  club: ClubDetailDto | undefined
  canManage: boolean
  canManageContact: boolean
}) {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const editDialog = useFormDialog()
  const contactDialog = useFormDialog()
  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting: isFormSubmitting },
  } = useForm<ClubFormValues>({
    resolver: zodResolver(clubFormSchema),
    defaultValues: emptyClubFormValues,
  })

  const {
    control: contactControl,
    handleSubmit: handleContactSubmit,
    reset: resetContact,
    formState: { isSubmitting: isContactFormSubmitting },
  } = useForm<ClubContactFormValues>({
    resolver: zodResolver(clubContactFormSchema),
    defaultValues: emptyClubContactFormValues,
  })

  const { fields: linkFields, append: appendLink, remove: removeLink } = useFieldArray({ control: contactControl, name: 'links' })

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
        clubCategoryIds: values.clubCategoryIds,
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
      clubCategoryIds: club?.clubCategoryIds ?? [],
    })
    editDialog.openDialog()
  }

  const setContactMutation = useMutation({
    mutationFn: async (values: ClubContactFormValues) => {
      await apiClient.put(`/clubs/${clubId}/contact`, toClubContactPayload(values))
    },
    onSuccess: () => {
      notify({ message: 'İletişim bilgileri güncellendi.', severity: 'success' })
      contactDialog.closeDialog()
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'İletişim bilgileri güncellenemedi.'), severity: 'error' }),
  })

  const openContactDialog = () => {
    resetContact({
      contactEmail: club?.contactEmail ?? '',
      contactPhone: club?.contactPhone ?? '',
      links: (club?.socialLinks ?? []).map((link) => ({ platform: link.platform, url: link.url })),
    })
    contactDialog.openDialog()
  }

  // §23.2: bomboş ekran yerine iskelet — üst bileşen `club` gelene kadar undefined geçer.
  if (!club) {
    return <Skeleton variant="rounded" height={280} />
  }

  return (
    <>
      <ClubProfileHeader
        name={club.name}
        logoFileId={club.logoFileId}
        badges={
          <>
            <Chip
              size="small"
              label={club.isActive ? 'Aktif' : 'Pasif'}
              color={club.isActive ? 'success' : 'default'}
            />
            {club.foundedYear !== null && (
              <Chip icon={<CalendarMonthOutlinedIcon />} label={`Kuruluş: ${club.foundedYear}`} />
            )}
            <Chip icon={<VisibilityOutlinedIcon />} label={`${club.viewCount.toLocaleString('tr-TR')} Görüntülenme`} />
            <Chip icon={<CalendarMonthOutlinedIcon />} label={`Kayıt: ${new Date(club.createdAtUtc).toLocaleDateString('tr-TR')}`} />
            {club.clubCategoryNames.map((name) => (
              <Chip key={name} variant="outlined" color="primary" label={name} />
            ))}
          </>
        }
        actions={
          canManage ? (
            <>
              <Button variant="outlined" onClick={openEditDialog}>
                Düzenle
              </Button>
              <Button
                color={club.isActive ? 'error' : 'success'}
                disabled={statusMutation.isPending}
                onClick={() => statusMutation.mutate(!club.isActive)}
              >
                {club.isActive ? 'Pasife Al' : 'Aktifleştir'}
              </Button>
            </>
          ) : undefined
        }
      />

      <Typography variant="body1" color="text.secondary" sx={{ mb: 3, lineHeight: 1.9, whiteSpace: 'pre-line' }}>
        {club.description || 'Bu topluluk için henüz açıklama eklenmemiş.'}
      </Typography>

      {/* K-44: iletişim e-postası/telefonu ve sosyal bağlantılar — üye olmayan ziyaretçiye de görünür (vitrindeki karşılığı PublicClubDetailPage). */}
      {(club.contactEmail || club.contactPhone || club.socialLinks.length > 0 || canManageContact) && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3, borderRadius: 3 }}>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              İletişim
            </Typography>
            {canManageContact && (
              <Button size="small" variant="outlined" onClick={openContactDialog}>
                Düzenle
              </Button>
            )}
          </Stack>

          {!club.contactEmail && !club.contactPhone && club.socialLinks.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Bu topluluk için henüz iletişim bilgisi eklenmemiş.
            </Typography>
          ) : (
            <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
              {club.contactEmail && (
                <Stack
                  direction="row"
                  spacing={0.5}
                  component="a"
                  href={`mailto:${club.contactEmail}`}
                  sx={{ alignItems: 'center', color: 'text.primary', textDecoration: 'none' }}
                >
                  <EmailOutlinedIcon fontSize="small" color="action" />
                  <Typography variant="body2">{club.contactEmail}</Typography>
                </Stack>
              )}
              {club.contactPhone && (
                <Stack
                  direction="row"
                  spacing={0.5}
                  component="a"
                  href={`tel:${club.contactPhone}`}
                  sx={{ alignItems: 'center', color: 'text.primary', textDecoration: 'none' }}
                >
                  <PhoneOutlinedIcon fontSize="small" color="action" />
                  <Typography variant="body2">{club.contactPhone}</Typography>
                </Stack>
              )}
              <SocialLinkIcons links={club.socialLinks} />
            </Stack>
          )}
        </Paper>
      )}

      <Dialog open={contactDialog.open} onClose={contactDialog.closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>İletişim Bilgilerini Düzenle</DialogTitle>
        <DialogContent>
          <Controller
            name="contactEmail"
            control={contactControl}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth margin="dense" label="İletişim E-postası" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="contactPhone"
            control={contactControl}
            render={({ field }) => <TextField {...field} fullWidth margin="dense" label="İletişim Telefonu" />}
          />

          <Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
            Sosyal Medya Bağlantıları
          </Typography>
          <Stack spacing={1.5}>
            {linkFields.map((field, index) => (
              <Stack key={field.id} direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
                <Controller
                  name={`links.${index}.platform`}
                  control={contactControl}
                  render={({ field: platformField }) => (
                    <TextField {...platformField} select sx={{ width: 160 }} margin="dense" label="Platform">
                      {SOCIAL_PLATFORMS.map((platform) => (
                        <MenuItem key={platform} value={platform}>
                          {SOCIAL_PLATFORM_LABELS[platform]}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
                <Controller
                  name={`links.${index}.url`}
                  control={contactControl}
                  render={({ field: urlField, fieldState }) => (
                    <TextField
                      {...urlField}
                      fullWidth
                      margin="dense"
                      label="Bağlantı (https://)"
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />
                <IconButton aria-label="Bağlantıyı kaldır" onClick={() => removeLink(index)} sx={{ mt: 1 }}>
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button
              variant="text"
              onClick={() => appendLink({ platform: 'Instagram', url: '' })}
              sx={{ alignSelf: 'flex-start' }}
              disabled={linkFields.length >= 10}
            >
              + Bağlantı Ekle
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={contactDialog.closeDialog}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={isContactFormSubmitting || setContactMutation.isPending}
            onClick={handleContactSubmit((values) => setContactMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

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
            name="clubCategoryIds"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                select
                fullWidth
                margin="dense"
                label="Kategoriler (en fazla 3)"
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
                slotProps={{ select: { multiple: true, renderValue: (selected) => (selected as number[]).length + ' kategori' } }}
              >
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
    </>
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
