import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Autocomplete, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { RegistrationDepartmentDto } from '../api/types'
import { AuthBrandPanel } from '../components/layout/AuthBrandPanel'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

const currentYear = new Date().getFullYear()

// Y-35: yalnızca biçim doğrulanır — src/Business/ValidationRules/PasswordRules.cs ile aynı kural
// (en az 6 karakter, büyük/küçük harf, rakam, alfanumerik olmayan bir karakter).
const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/

type RegisterFormValues = {
  firstName: string
  lastName: string
  email: string
  password: string
  studentNumber: string
  departmentId: number
  enrollmentYear: number
}

export function RegisterPage() {
  const { t } = useLocale()
  useDocumentTitle(t('auth.registerTitle'))

  const registerSchema = useMemo(
    () =>
      z.object({
        firstName: z.string().max(100, t('validation.maxChars')),
        lastName: z.string().max(100, t('validation.maxChars')),
        email: z.string().min(1, t('validation.emailRequired')).email(t('validation.emailInvalid')),
        password: z.string().regex(strongPasswordRegex, t('validation.passwordStrong')),
        studentNumber: z.string().min(1, t('validation.studentNumber')),
        departmentId: z.number({ error: t('validation.department') }).int().positive(t('validation.department')),
        enrollmentYear: z.number().int().min(2000).max(currentYear + 1),
      }),
    [t],
  )

  const [serverError, setServerError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const departmentsQuery = useQuery({
    queryKey: ['registration-departments'],
    queryFn: async () => (await apiClient.get<RegistrationDepartmentDto[]>('/auth/departments')).data,
  })

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { firstName: '', lastName: '', email: '', password: '', studentNumber: '', departmentId: 0, enrollmentYear: currentYear },
  })

  const onSubmit = async (values: RegisterFormValues) => {
    setServerError(null)
    try {
      const response = await apiClient.post<{ message?: string }>('/auth/register', values)
      setSuccessMessage(response.data.message ?? t('auth.registerOk'))
      reset()
    } catch (error) {
      setServerError(extractErrorMessage(error, t('auth.registerFail')))
    }
  }

  return (
    <Box sx={{ flex: 1, display: 'flex' }}>
      <AuthBrandPanel
        title={t('auth.registerPanelTitle')}
        subtitle={t('auth.registerPanelLead')}
      />

      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: { xs: 2.5, md: 4 }, overflowY: 'auto' }}>
        <Box sx={{ width: '100%', maxWidth: 440, py: 2 }}>
          <AuthFormCard>
          <BackButton to="/" label={t('common.backHome')} />
          <Box sx={{ display: { xs: 'block', md: 'none' }, mb: 3, mt: 1 }}>
            <BrandMark to="/" showSubtitle />
          </Box>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mb: 0.75, fontSize: 28 }}>
            {t('auth.registerTitle')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            {t('auth.registerLead')}
          </Typography>

          {successMessage ? (
            <Stack spacing={2}>
              <Alert severity="success">{successMessage}</Alert>
              <Button component={RouterLink} to="/login" variant="outlined">
                {t('common.backLogin')}
              </Button>
            </Stack>
          ) : (
            <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Stack direction="row" spacing={2}>
                <Controller
                  name="firstName"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField {...field} label={t('auth.firstName')} autoComplete="given-name" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                  )}
                />
                <Controller
                  name="lastName"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField {...field} label={t('auth.lastName')} autoComplete="family-name" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                  )}
                />
              </Stack>

              <Controller
                name="email"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label={t('auth.email')} type="email" autoComplete="username" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              <Controller
                name="password"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label={t('auth.password')} type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              <Controller
                name="studentNumber"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label={t('auth.studentNumber')} error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              {/*
                K-34: liste 1 bölümden 121'e çıktı — düz bir Select'te kaydırmadan bulunamaz.
                Autocomplete istemcide arar; Y-62 ihlali değildir çünkü bu sunucu sayfalı bir
                domain listesi değil, tek seferde inen SINIRLI referans verisidir (kullanıcı
                henüz oturum açmamıştır, arama ucu çağıramaz).
              */}
              <Controller
                name="departmentId"
                control={control}
                render={({ field, fieldState }) => (
                  <Autocomplete
                    options={departmentsQuery.data ?? []}
                    loading={departmentsQuery.isPending}
                    loadingText={t('auth.departmentsLoading')}
                    noOptionsText={t('auth.departmentsEmpty')}
                    groupBy={(option) => option.facultyName}
                    getOptionLabel={(option) => option.name}
                    isOptionEqualToValue={(option, selected) => option.id === selected.id}
                    value={(departmentsQuery.data ?? []).find((dept) => dept.id === field.value) ?? null}
                    onChange={(_, selected) => field.onChange(selected?.id ?? 0)}
                    onBlur={field.onBlur}
                    renderInput={(params) => (
                      <TextField
                        {...params}
                        label={t('auth.department')}
                        error={!!fieldState.error}
                        helperText={fieldState.error?.message}
                      />
                    )}
                    fullWidth
                  />
                )}
              />

              <Controller
                name="enrollmentYear"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    type="number"
                    label={t('auth.enrollmentYear')}
                    onChange={(event) => field.onChange(Number(event.target.value))}
                    error={!!fieldState.error}
                    helperText={fieldState.error?.message}
                    fullWidth
                  />
                )}
              />

              {serverError && <Alert severity="error">{serverError}</Alert>}

              <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
                {t('auth.registerTitle')}
              </Button>

              <Button component={RouterLink} to="/login" variant="text" size="small">
                {t('auth.alreadyAccount')}
              </Button>
            </Box>
          )}
          </AuthFormCard>
        </Box>
      </Box>
    </Box>
  )
}
