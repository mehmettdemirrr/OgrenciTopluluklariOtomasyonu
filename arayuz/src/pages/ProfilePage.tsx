import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Chip, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { MeResponseDto, RegistrationDepartmentDto } from '../api/types'

const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/
const currentYear = new Date().getFullYear()

const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Mevcut parolanızı girin.'),
  newPassword: z.string().regex(strongPasswordRegex, 'Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.'),
})

type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>

// Y-35: yalnızca biçim — bölümün var olup olmadığı API'nin kararı.
const profileSchema = z.object({
  departmentId: z.number({ error: 'Bölüm seçin.' }).int().positive('Bölüm seçin.'),
  enrollmentYear: z.number().int().min(2000).max(currentYear + 1),
})

type ProfileFormValues = z.infer<typeof profileSchema>

export function ProfilePage() {
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
    defaultValues: { departmentId: 0, enrollmentYear: currentYear },
  })

  // /me yüklendiğinde formu mevcut değerlerle doldur.
  const { reset: resetProfileForm } = profileForm
  useEffect(() => {
    if (meQuery.data?.departmentId != null && meQuery.data.enrollmentYear != null) {
      resetProfileForm({ departmentId: meQuery.data.departmentId, enrollmentYear: meQuery.data.enrollmentYear })
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

  return (
    <>
      <PageHeader title="Profilim" description="Hesap bilgileriniz ve parola yönetimi." />

      <Stack spacing={3}>
        <SectionCard title="Hesap Bilgileri">
          <Stack spacing={1.5}>
            <Typography variant="body2">{meQuery.data?.email}</Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
              {(meQuery.data?.roles ?? []).map((role) => (
                <Chip key={role} size="small" label={role} />
              ))}
            </Stack>
          </Stack>
        </SectionCard>

        {isStudent && (
          <SectionCard title="Öğrenci Bilgilerim">
            <Stack
              component="form"
              spacing={2}
              onSubmit={profileForm.handleSubmit((values) => updateProfileMutation.mutate(values))}
              sx={{ maxWidth: 420 }}
            >
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
