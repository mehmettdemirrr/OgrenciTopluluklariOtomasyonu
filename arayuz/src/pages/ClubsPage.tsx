import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  MenuItem,
  Typography,
} from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useRef, useState, type ChangeEvent } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { useFormDialog } from '../hooks/useFormDialog'
import { useSearchPagedQuery } from '../hooks/useSearchPagedQuery'
import { useNotifier } from '../notifications/NotifierProvider'
import { CardGridSkeleton } from '../components/ui/CardGridSkeleton'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { ResultPagination } from '../components/ui/ResultPagination'
import { SearchField } from '../components/ui/SearchField'
import { createClubFormSchema, emptyCreateClubFormValues, toCategoryPayload, type CreateClubFormValues } from '../schemas/clubForm'
import { clubApplicationFormSchema, emptyClubApplicationFormValues, type ClubApplicationFormValues } from '../schemas/clubApplicationForm'
import type {
  AcademicStaffListItemDto,
  ClubApplicationWindowDto,
  ClubCategoryListItemDto,
  ClubListItemDto,
  PagedResult,
  SelectableAcademicStaffDto,
} from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

type StatusFilter = 'all' | 'active' | 'inactive'

export function ClubsPage() {
  useDocumentTitle('Kulüpler')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const { hasPermission } = useAuth()
  const canUploadLogo = hasPermission(Permissions.FilesUpload)
  const canManageClubs = hasPermission(Permissions.ClubsWrite)

  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  // K-35: 0 = tüm kategoriler.
  const [categoryFilter, setCategoryFilter] = useState<number>(0)
  const logoInputRef = useRef<HTMLInputElement>(null)
  const [logoTargetClubId, setLogoTargetClubId] = useState<number | null>(null)
  const createDialog = useFormDialog()

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting: isFormSubmitting },
  } = useForm<CreateClubFormValues>({
    resolver: zodResolver(createClubFormSchema),
    defaultValues: emptyCreateClubFormValues,
  })

  // A-50/Y-62: arama ve aktif/pasif filtresi sunucuda. "Pasif" düğmesi önceden hiçbir zaman
  // sonuç vermiyordu — /api/clubs pasif kulüpleri hiç dönmüyordu (bkz. PLAN-V4 §21.1b).
  const { search, setSearch, items: clubs, pageIndex, setPageIndex, pageCount, totalCount, query: clubsQuery } =
    useSearchPagedQuery<ClubListItemDto>({
      // categoryFilter queryKey'de OLMAK ZORUNDA — olmasaydı TanStack Query eski sonucu
      // önbellekten servis eder ve filtre çalışmıyormuş gibi görünürdü.
      queryKey: ['clubs', statusFilter, categoryFilter],
      queryFn: async ({ pageIndex: page, pageSize, search: term }) =>
        (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
          params: {
            pageIndex: page,
            pageSize,
            search: term || undefined,
            isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
            categoryId: categoryFilter > 0 ? categoryFilter : undefined,
          },
        })).data,
    })

  const categoriesQuery = useQuery({
    queryKey: ['club-categories'],
    queryFn: async () =>
      (await apiClient.get<PagedResult<ClubCategoryListItemDto>>('/club-categories', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })

  const applyDialog = useFormDialog()
  const applyForm = useForm<ClubApplicationFormValues>({
    resolver: zodResolver(clubApplicationFormSchema),
    defaultValues: emptyClubApplicationFormValues,
  })

  // K-39: pencerenin durumu sunucudan gelir. Arayüz tarihlere bakıp kendi kararını VERMEZ (Y-73).
  const windowQuery = useQuery({
    queryKey: ['club-application-window'],
    queryFn: async () => (await apiClient.get<ClubApplicationWindowDto>('/club-applications/window')).data,
  })

  const submitClubApplicationMutation = useMutation({
    mutationFn: async (values: ClubApplicationFormValues) => {
      await apiClient.post('/club-applications', {
        proposedName: values.proposedName,
        description: values.description.trim() || null,
        justification: values.justification,
        proposedAdvisorId: values.proposedAdvisorId,
        proposedCategoryId: toCategoryPayload(values.proposedCategoryId),
      })
    },
    onSuccess: () => {
      notify({ message: 'Topluluk kurma başvurunuz alındı, yönetici onayı bekleniyor.', severity: 'success' })
      applyDialog.closeDialog()
      applyForm.reset(emptyClubApplicationFormValues)
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const applyMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.post(`/clubs/${clubId}/membership-applications`)
    },
    onSuccess: () => {
      notify({ message: 'Başvurunuz alındı, danışman onayı bekleniyor.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['membership-applications'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const logoMutation = useMutation({
    mutationFn: async ({ clubId, file }: { clubId: number; file: File }) => {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post(`/clubs/${clubId}/logo`, formData)
    },
    onSuccess: () => {
      notify({ message: 'Logo güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Logo yüklenemedi.'), severity: 'error' }),
  })

  const createClubMutation = useMutation({
    mutationFn: async (values: CreateClubFormValues) => {
      await apiClient.post('/clubs', {
        name: values.name.trim(),
        description: values.description.trim() || null,
        advisorId: values.advisorId,
        clubCategoryId: toCategoryPayload(values.clubCategoryId),
      })
    },
    onSuccess: () => {
      notify({ message: 'Topluluk oluşturuldu.', severity: 'success' })
      createDialog.closeDialog()
      reset(emptyCreateClubFormValues)
      queryClient.invalidateQueries({ queryKey: ['clubs'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Topluluk oluşturulamadı.'), severity: 'error' }),
  })

  const handleLogoButtonClick = (clubId: number) => {
    setLogoTargetClubId(clubId)
    logoInputRef.current?.click()
  }

  const handleLogoFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (file && logoTargetClubId !== null) {
      logoMutation.mutate({ clubId: logoTargetClubId, file })
    }
  }

  return (
    <>
      <PageHeader
        title="Kulüpler"
        description="Kampüsteki tüm öğrenci topluluklarını keşfedin ve üyelik başvurusu yapın."
        action={
          <Stack direction="row" spacing={1}>
            {/* Y-35: düğmeyi pasifleştirmek bir KOLAYLIK, yetki değil — API muhafızı her hâlükârda
                409 döner. isOpen !== true kasıtlı: veri henüz gelmemişken (undefined) düğme pasif
                kalır, fail-closed'ın arayüz karşılığı (A-66). */}
            <Tooltip
              title={
                windowQuery.data && !windowQuery.data.isOpen
                  ? windowQuery.data.startUtc
                    ? `Başvurular ${new Date(windowQuery.data.startUtc).toLocaleDateString('tr-TR')} tarihinde açılıyor.`
                    : 'Topluluk kurma başvuruları şu anda kapalı.'
                  : ''
              }
            >
              <span>
                <Button variant="outlined" disabled={windowQuery.data?.isOpen !== true} onClick={applyDialog.openDialog}>
                  Topluluk Kurmak İstiyorum
                </Button>
              </span>
            </Tooltip>
            {canManageClubs && (
              <Button variant="contained" onClick={createDialog.openDialog}>
                Yeni Topluluk
              </Button>
            )}
          </Stack>
        }
      />

      <input ref={logoInputRef} type="file" accept="image/*" hidden onChange={handleLogoFileChange} />

      <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
        <SearchField value={search} onChange={setSearch} placeholder="Kulüp ara…" />
        <ToggleButtonGroup
          exclusive
          size="small"
          value={statusFilter}
          onChange={(_, value: StatusFilter | null) => {
            if (value) {
              setStatusFilter(value)
              setPageIndex(0)
            }
          }}
        >
          <ToggleButton value="all">Tümü</ToggleButton>
          <ToggleButton value="active">Aktif</ToggleButton>
          <ToggleButton value="inactive">Pasif</ToggleButton>
        </ToggleButtonGroup>
        {/* A-50/Y-62: filtre SUNUCUDA — categoryId sorguya gider, istemcide ayıklama yok. */}
        <TextField
          select
          size="small"
          label="Kategori"
          value={categoryFilter}
          onChange={(event) => {
            setCategoryFilter(Number(event.target.value))
            setPageIndex(0)
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value={0}>Tüm kategoriler</MenuItem>
          {(categoriesQuery.data?.items ?? []).map((category) => (
            <MenuItem key={category.id} value={category.id}>
              {category.name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {clubsQuery.isLoading && <CardGridSkeleton withMedia />}

      {!clubsQuery.isLoading && clubs.length === 0 && (
        <EmptyState icon={GroupsOutlinedIcon} title="Kulüp bulunamadı" description="Arama veya filtre kriterlerinizi değiştirmeyi deneyin." />
      )}

      <Grid container spacing={2}>
        {clubs.map((club) => (
          <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              {club.logoFileId ? (
                // A-36: açık görsel, anonim uçtan doğrudan <img src> ile — tarayıcı önbelleği çalışır.
                <CardMedia component="img" height={120} image={`/api/files/${club.logoFileId}`} alt="" sx={{ objectFit: 'contain', bgcolor: 'grey.50', p: 2 }} />
              ) : (
                <Box sx={{ height: 120, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50' }}>
                  <GroupsOutlinedIcon sx={{ fontSize: 40, color: 'grey.400' }} />
                </Box>
              )}
              <CardContent sx={{ flex: 1 }}>
                <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }} noWrap>
                    {club.name}
                  </Typography>
                  <Chip size="small" label={club.isActive ? 'Aktif' : 'Pasif'} color={club.isActive ? 'success' : 'default'} variant={club.isActive ? 'filled' : 'outlined'} />
                </Stack>
                {club.clubCategoryName && (
                  <Chip size="small" variant="outlined" label={club.clubCategoryName} sx={{ mb: 1 }} />
                )}
                <Typography variant="body2" color="text.secondary" sx={{ display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}>
                  {club.description || 'Açıklama eklenmemiş.'}
                </Typography>
              </CardContent>
              <CardActions sx={{ px: 2, pb: 2, flexWrap: 'wrap', gap: 0.5 }}>
                <Button size="small" component={RouterLink} to={`/clubs/${club.id}`}>
                  Detay
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  disabled={!club.isActive || applyMutation.isPending}
                  onClick={() => applyMutation.mutate(club.id)}
                >
                  Başvur
                </Button>
                {canUploadLogo && (
                  <Button size="small" disabled={logoMutation.isPending} onClick={() => handleLogoButtonClick(club.id)}>
                    Logo Yükle
                  </Button>
                )}
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      <ResultPagination pageIndex={pageIndex} pageCount={pageCount} totalCount={totalCount} onChange={setPageIndex} />

      <Dialog
        open={createDialog.open}
        onClose={() => {
          createDialog.closeDialog()
          reset(emptyCreateClubFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Topluluk</DialogTitle>
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
          <Controller
            name="advisorId"
            control={control}
            render={({ field, fieldState }) => (
              <RemoteSelect<AcademicStaffListItemDto>
                label="Danışman"
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? 0)}
                queryKey={['academic-staff']}
                enabled={canManageClubs && createDialog.open}
                fetchOptions={async (term) =>
                  (await apiClient.get<PagedResult<AcademicStaffListItemDto>>('/academic-staff', {
                    params: { pageIndex: 0, pageSize: 20, search: term || undefined },
                  })).data.items
                }
                getOptionId={(staff) => staff.id}
                getOptionLabel={(staff) => `${staff.title} — ${staff.email}`}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
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
          <Button
            onClick={() => {
              createDialog.closeDialog()
              reset(emptyCreateClubFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={isFormSubmitting || createClubMutation.isPending}
            onClick={handleSubmit((values) => createClubMutation.mutate(values))}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={applyDialog.open}
        onClose={() => {
          applyDialog.closeDialog()
          applyForm.reset(emptyClubApplicationFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Topluluk Kurma Başvurusu</DialogTitle>
        <DialogContent>
          <Controller
            name="proposedName"
            control={applyForm.control}
            render={({ field, fieldState }) => (
              <TextField {...field} autoFocus fullWidth margin="dense" label="Topluluk Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="description"
            control={applyForm.control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={2} margin="dense" label="Açıklama" />}
          />
          <Controller
            name="justification"
            control={applyForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                multiline
                minRows={2}
                margin="dense"
                label="Gerekçe"
                helperText={fieldState.error?.message ?? 'Bu topluluk neden gerekli?'}
                error={!!fieldState.error}
              />
            )}
          />
          {/* K-29/A-45: reference.manage değil — herhangi bir kimliği doğrulanmış öğrenci danışman seçebilsin diye dar uç. */}
          <Controller
            name="proposedAdvisorId"
            control={applyForm.control}
            render={({ field, fieldState }) => (
              <RemoteSelect<SelectableAcademicStaffDto>
                label="Danışman"
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? 0)}
                queryKey={['academic-staff-selectable']}
                enabled={applyDialog.open}
                fetchOptions={async (term) =>
                  (await apiClient.get<PagedResult<SelectableAcademicStaffDto>>('/academic-staff/selectable', {
                    params: { pageIndex: 0, pageSize: 20, search: term || undefined },
                  })).data.items
                }
                getOptionId={(staff) => staff.id}
                getOptionLabel={(staff) => `${staff.title} ${staff.fullName}`}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
          <Controller
            name="proposedCategoryId"
            control={applyForm.control}
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
          <Button
            onClick={() => {
              applyDialog.closeDialog()
              applyForm.reset(emptyClubApplicationFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={applyForm.formState.isSubmitting || submitClubApplicationMutation.isPending}
            onClick={applyForm.handleSubmit((values) => submitClubApplicationMutation.mutate(values))}
          >
            Başvur
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
