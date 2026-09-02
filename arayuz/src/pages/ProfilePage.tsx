import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Avatar, Button, Chip, Grid, MenuItem, Stack, TextField } from '@mui/material'
import BadgeOutlinedIcon from '@mui/icons-material/BadgeOutlined'
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { DetailHero } from '../components/ui/DetailHero'
import { InfoTile } from '../components/ui/InfoTile'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { MeResponseDto, RegistrationDepartmentDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/
const currentYear = new Date().getFullYear()

const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Mevcut parolanızı girin.'),
  newPassword: z.string().regex(strongPasswordRegex, 'Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.'),
})

type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>

// Y-35: yalnızca biçim — bölümün var olup olmadığı API'nin kararı.
const profileSchema = z.object({
  // A-56: mevcut hesaplarda ad soyad boş başlar — doldurmanın tek yolu bu form.
  firstName: z.string().max(100, 'En fazla 100 karakter.'),
  lastName: z.string().max(100, 'En fazla 100 karakter.'),
  departmentId: z.number({ error: 'Bölüm seçin.' }).int().positive('Bölüm seçin.'),
  enrollmentYear: z.number().int().min(2000).max(currentYear + 1),
})

type ProfileFormValues = z.infer<typeof profileSchema>

export function ProfilePage() {
  const { t } = useLocale()
  useDocumentTitle(t('profile.title'))

  const notify = useNotifier()
  const queryClient = useQueryClient()

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: async () => (await apiClient.get<MeResponseDto>('/me')).data,
  })

  const isStudent = meQuery.data?.studentNumber != null

  const departmentsQuery = useQuery({
    queryKey: ['registration-departments'],
    enabled: isStudent,
    queryFn: async () => (await apiClient.get<RegistrationDepartmentDto[]>('/auth/departments')).data,
  })

  const profileForm = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { firstName: '', lastName: '', departmentId: 0, enrollmentYear: currentYear },
  })

  // /me yüklendiğinde formu mevcut değerlerle doldur.
  const { reset: resetProfileForm } = profileForm
  useEffect(() => {
    if (meQuery.data?.departmentId != null && meQuery.data.enrollmentYear != null) {
      resetProfileForm({
        firstName: meQuery.data.firstName ?? '',
        lastName: meQuery.data.lastName ?? '',
        departmentId: meQuery.data.departmentId,
        enrollmentYear: meQuery.data.enrollmentYear,
      })
    }
  }, [meQuery.data, resetProfileForm])

  const updateProfileMutation = useMutation({
    mutationFn: async (values: ProfileFormValues) => {
      await apiClient.put('/me', values)
    },
    onSuccess: () => {
      notify({ message: 'Profil bilgileriniz güncellendi.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['me'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Profil güncellenemedi.'), severity: 'error' }),
  })

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting },
  } = useForm<ChangePasswordFormValues>({ resolver: zodResolver(changePasswordSchema), defaultValues: { currentPassword: '', newPassword: '' } })

  const changePasswordMutation = useMutation({
    mutationFn: async (values: ChangePasswordFormValues) => {
      await apiClient.post('/auth/change-password', values)
    },
    onSuccess: () => {
      notify({ message: 'Parolanız güncellendi.', severity: 'success' })
      reset()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Parola güncellenemedi.'), severity: 'error' }),
  })

  const me = meQuery.data
  const displayName = [me?.firstName, me?.lastName].filter(Boolean).join(' ')
  const avatarLetter = (displayName || me?.email || '?').charAt(0).toUpperCase()

  return (
    <>
      <PageHeader title={t('profile.title')} description={t('profile.lead')} backTo="/panel" />

      <Stack spacing={3}>
        <DetailHero
          media={
            <Avatar
              sx={{
                width: { xs: 88, md: 112 },
                height: { xs: 88, md: 112 },
                bgcolor: 'primary.dark',
                fontWeight: 800,
                fontSize: { xs: 32, md: 40 },
              }}
            >
              {avatarLetter}
            </Avatar>
          }
          chips={
            (me?.roles ?? []).length > 0
              ? (me?.roles ?? []).map((role) => (
                  <Chip key={role} size="small" color="primary" variant="outlined" label={role} />
                ))
              : undefined
          }
          title={displayName || me?.email || 'Hesap'}
          subtitle={displayName ? me?.email : undefined}
        >
          {isStudent && (
            <Grid container spacing={1.5}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <InfoTile icon={BadgeOutlinedIcon} label="Öğrenci No" value={me?.studentNumber ?? '—'} />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <InfoTile
                  icon={SchoolOutlinedIcon}
                  label="Bölüm"
                  value={me?.departmentName ? `${me.departmentName}${me.facultyName ? ` · ${me.facultyName}` : ''}` : '—'}
                />
              </Grid>
            </Grid>
          )}
        </DetailHero>

        {isStudent && (
          <SectionCard title="Öğrenci Bilgilerim">
            <Stack
              component="form"
              spacing={2}
              onSubmit={profileForm.handleSubmit((values) => updateProfileMutation.mutate(values))}
              sx={{ maxWidth: 420 }}
            >
              {/* A-56: ad soyad — öğrenci numarasının aksine kullanıcının kendi düzeltebileceği alan. */}
              <Stack direction="row" spacing={2}>
                <Controller
                  name="firstName"
                  control={profileForm.control}
                  render={({ field, fieldState }) => (
                    <TextField {...field} label="Ad" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                  )}
                />
                <Controller
                  name="lastName"
                  control={profileForm.control}
                  render={({ field, fieldState }) => (
                    <TextField {...field} label="Soyad" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                  )}
                />
              </Stack>

              {/* §19.2: öğrenci numarası kimliğin parçası — salt okunur, DTO'da alanı bile yok. */}
              <TextField
                label="Öğrenci Numarası"
                value={meQuery.data?.studentNumber ?? ''}
                slotProps={{ input: { readOnly: true } }}
                helperText="Öğrenci numarası değiştirilemez; hatalıysa yöneticinize başvurun."
                fullWidth
              />

              <Controller
                name="departmentId"
                control={profileForm.control}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    select
                    label="Bölüm"
                    value={field.value || ''}
                    onChange={(event) => field.onChange(Number(event.target.value))}
                    error={!!fieldState.error}
                    helperText={fieldState.error?.message}
                    fullWidth
                  >
                    {(departmentsQuery.data ?? []).map((department) => (
                      <MenuItem key={department.id} value={department.id}>
                        {department.name} ({department.facultyName})
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />

              <Controller
                name="enrollmentYear"
                control={profileForm.control}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Kayıt Yılı"
                    onChange={(event) => field.onChange(Number(event.target.value))}
                    error={!!fieldState.error}
                    helperText={fieldState.error?.message}
                    fullWidth
                  />
                )}
              />

              <Button
                type="submit"
                variant="contained"
                disabled={profileForm.formState.isSubmitting || updateProfileMutation.isPending}
                sx={{ alignSelf: 'flex-start' }}
              >
                Bilgilerimi Güncelle
              </Button>
            </Stack>
          </SectionCard>
        )}

        <SectionCard title="Parola Değiştir">
          <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => changePasswordMutation.mutate(values))} sx={{ maxWidth: 380 }}>
            <Controller
              name="currentPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label="Mevcut Parola" type="password" autoComplete="current-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />
            <Controller
              name="newPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label="Yeni Parola" type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />
            {changePasswordMutation.isError && (
              <Alert severity="error">{extractErrorMessage(changePasswordMutation.error, 'Parola güncellenemedi.')}</Alert>
            )}
            <Button type="submit" variant="contained" disabled={isSubmitting || changePasswordMutation.isPending} sx={{ alignSelf: 'flex-start' }}>
              Parolayı Güncelle
            </Button>
          </Stack>
        </SectionCard>
      </Stack>
    </>
  )
}
