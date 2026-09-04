import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Avatar, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { PageHeader } from '../components/ui/PageHeader'
import { RemoteSelect } from '../components/ui/RemoteSelect'
import { SectionCard } from '../components/ui/SectionCard'
import {
  clubApplicationFormSchema,
  emptyClubApplicationFormValues,
  type ClubApplicationFormValues,
} from '../schemas/clubApplicationForm'
import { toCategoryPayload } from '../schemas/clubForm'
import type {
  ClubApplicationWindowDto,
  ClubCategoryListItemDto,
  ClubDocumentTypeListItemDto,
  PagedResult,
  SelectableAcademicStaffDto,
} from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function ClubApplicationPage() {
  useDocumentTitle('Topluluk Kuruluş Başvurusu')

  const navigate = useNavigate()
  const notify = useNotifier()
  const [files, setFiles] = useState<Record<number, File>>({})
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null)

  // K-40: seçim anında yeni obje URL'i kurulur, eskisi hemen serbest bırakılır.
  const handleLogoChange = (file: File | null) => {
    setLogoFile(file)
    setLogoPreviewUrl((previous) => {
      if (previous) {
        URL.revokeObjectURL(previous)
      }
      return file ? URL.createObjectURL(file) : null
    })
  }

  // Yalnızca bileşen kapanırken kalan URL'i serbest bırakır — değişimdeki revoke handleLogoChange'te.
  useEffect(() => () => {
    if (logoPreviewUrl) {
      URL.revokeObjectURL(logoPreviewUrl)
    }
  }, [logoPreviewUrl])

  const { control, handleSubmit } = useForm<ClubApplicationFormValues>({
    resolver: zodResolver(clubApplicationFormSchema),
    defaultValues: emptyClubApplicationFormValues,
  })

  // K-39: pencerenin durumu sunucudan gelir. Arayüz tarihlere bakıp kendi kararını VERMEZ (Y-73).
  const windowQuery = useQuery({
    queryKey: ['club-application-window'],
    queryFn: async () => (await apiClient.get<ClubApplicationWindowDto>('/club-applications/window')).data,
  })

  const documentTypesQuery = useQuery({
    queryKey: ['club-document-types', 'active'],
    queryFn: async () =>
      (
        await apiClient.get<PagedResult<ClubDocumentTypeListItemDto>>('/club-document-types', {
          params: { pageIndex: 0, pageSize: 100, activeOnly: true },
        })
      ).data,
  })

  const categoriesQuery = useQuery({
    queryKey: ['club-categories'],
    queryFn: async () =>
      (await apiClient.get<PagedResult<ClubCategoryListItemDto>>('/club-categories', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })

  const submitMutation = useMutation({
    mutationFn: async (values: ClubApplicationFormValues) => {
      const formData = new FormData()
      formData.append('ProposedName', values.proposedName)
      formData.append('Description', values.description)
      formData.append('Justification', values.justification)
      formData.append('ProposedAdvisorId', String(values.proposedAdvisorId))

      const categoryId = toCategoryPayload(values.proposedCategoryId)
      if (categoryId !== null) {
        formData.append('ProposedCategoryId', String(categoryId))
      }

      if (logoFile) {
        formData.append('Logo', logoFile)
      }

      // Alan adları backend'in bağlama modeliyle birebir: Documents[i].DocumentTypeId / .File
      Object.entries(files).forEach(([typeId, file], index) => {
        formData.append(`Documents[${index}].DocumentTypeId`, typeId)
        formData.append(`Documents[${index}].File`, file)
      })

      await apiClient.post('/club-applications', formData)
    },
    onSuccess: () => {
      notify({ message: 'Başvurunuz alındı, yönetici onayı bekleniyor.', severity: 'success' })
      navigate('/my-applications')
    },
    // Y-35: "hangi evrak eksik" kararı API'nin; mesajı olduğu gibi gösteriyoruz.
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const documentTypes = documentTypesQuery.data?.items ?? []
  const windowClosed = windowQuery.data !== undefined && !windowQuery.data.isOpen

  return (
    <>
      <PageHeader
        title="Yeni Topluluk Kuruluş Başvurusu"
        description="Tüm alanları eksiksiz doldurun. Zorunlu evrakların tamamı PDF olarak yüklenmelidir."
        backTo="/clubs"
      />

      {windowClosed && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {windowQuery.data?.startUtc
            ? `Topluluk kurma başvuruları şu anda kapalı. Başvurular ${new Date(windowQuery.data.startUtc).toLocaleDateString('tr-TR')} tarihinde açılıyor.`
            : 'Topluluk kurma başvuruları şu anda kapalı.'}
        </Alert>
      )}

      <SectionCard title="TOPLULUK BİLGİLERİ">
        <Stack spacing={2}>
          <Controller
            name="proposedName"
            control={control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth required label="Topluluk Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="description"
            control={control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={3} label="Topluluk Açıklaması" />}
          />
          <Controller
            name="justification"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                required
                multiline
                minRows={2}
                label="Gerekçe"
                error={!!fieldState.error}
                helperText={fieldState.error?.message ?? 'Bu topluluk neden gerekli?'}
              />
            )}
          />
          {/* K-29/A-45: reference.manage değil — herhangi bir kimliği doğrulanmış öğrenci danışman seçebilsin diye dar uç. */}
          <Controller
            name="proposedAdvisorId"
            control={control}
            render={({ field, fieldState }) => (
              <RemoteSelect<SelectableAcademicStaffDto>
                label="Akademik Danışman"
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? 0)}
                queryKey={['academic-staff-selectable']}
                fetchOptions={async (term) =>
                  (
                    await apiClient.get<PagedResult<SelectableAcademicStaffDto>>('/academic-staff/selectable', {
                      params: { pageIndex: 0, pageSize: 20, search: term || undefined },
                    })
                  ).data.items
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
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                select
                fullWidth
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
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
              Topluluk Logosu (isteğe bağlı)
            </Typography>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar src={logoPreviewUrl ?? undefined} variant="rounded" sx={{ width: 64, height: 64 }} />
              <Box>
                <input
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  onChange={(event) => handleLogoChange(event.target.files?.[0] ?? null)}
                />
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                  JPEG, PNG veya WebP. Onaylanırsa topluluğun logosu olur.
                </Typography>
              </Box>
            </Stack>
          </Box>
        </Stack>
      </SectionCard>

      <SectionCard title="ZORUNLU EVRAKLAR" sx={{ mt: 2 }}>
        <Alert severity="info" sx={{ mb: 2 }}>
          Evraklar yalnızca PDF olarak yüklenebilir. Dosya başına en fazla 5 MB.
        </Alert>

        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
          {documentTypes.map((type) => (
            <Box key={type.id} sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
                {type.code} {type.name} {type.isRequired && <Box component="span" sx={{ color: 'error.main' }}>*</Box>}
              </Typography>
              <input
                type="file"
                accept="application/pdf"
                onChange={(event) => {
                  const file = event.target.files?.[0]
                  setFiles((current) => {
                    const next = { ...current }
                    if (file) {
                      next[type.id] = file
                    } else {
                      delete next[type.id]
                    }
                    return next
                  })
                }}
              />
              {files[type.id] && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                  {files[type.id].name}
                </Typography>
              )}
            </Box>
          ))}
        </Box>
      </SectionCard>

      {/* Y-35: eksik evrak kararı API'nin — düğme yalnızca pencere kapalıyken pasif (A-66 fail-closed),
          ki bu da bir kolaylık; muhafız her hâlükârda sunucuda. */}
      <Box sx={{ mt: 3 }}>
        <Button
          variant="contained"
          size="large"
          disabled={submitMutation.isPending || windowQuery.data?.isOpen !== true}
          onClick={handleSubmit((values) => submitMutation.mutate(values))}
        >
          Başvuruyu Gönder
        </Button>
      </Box>
    </>
  )
}
