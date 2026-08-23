import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { RegistrationDepartmentDto } from '../api/types'
import logo from '../assets/logo.png'

const currentYear = new Date().getFullYear()

// Y-35: yalnızca biçim doğrulanır — src/Business/ValidationRules/PasswordRules.cs ile aynı kural
// (en az 6 karakter, büyük/küçük harf, rakam, alfanumerik olmayan bir karakter).
const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/

const registerSchema = z.object({
  email: z.string().min(1, 'E-posta gerekli.').email('Geçerli bir e-posta girin.'),
  password: z.string().regex(strongPasswordRegex, 'Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.'),
  studentNumber: z.string().min(1, 'Öğrenci numarası gerekli.'),
  departmentId: z.number({ error: 'Bölüm seçin.' }).int().positive('Bölüm seçin.'),
  enrollmentYear: z.number().int().min(2000).max(currentYear + 1),
})

type RegisterFormValues = z.infer<typeof registerSchema>

export function RegisterPage() {
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
    defaultValues: { email: '', password: '', studentNumber: '', departmentId: 0, enrollmentYear: currentYear },
  })

  const onSubmit = async (values: RegisterFormValues) => {
    setServerError(null)
    try {
      const response = await apiClient.post<{ message?: string }>('/auth/register', values)
      setSuccessMessage(response.data.message ?? 'Kaydınız alındı. E-postanızı kontrol edin.')
      reset()
    } catch (error) {
      setServerError(extractErrorMessage(error, 'Kayıt oluşturulamadı.'))
    }
  }

  return (
    <Box sx={{ flex: 1, display: 'flex' }}>
      <Box
        sx={{
          display: { xs: 'none', md: 'flex' },
          flexDirection: 'column',
          justifyContent: 'space-between',
          width: '42%',
          bgcolor: 'secondary.main',
          color: 'common.white',
          p: 6,
        }}
      >
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <Box component="img" src={logo} alt="" sx={{ width: 40, height: 40 }} />
          <Typography variant="h6" sx={{ fontWeight: 800 }}>
            Öğrenci Toplulukları
          </Typography>
        </Stack>

        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700, mb: 2 }}>
            Topluluğuna katıl.
          </Typography>
          <Typography variant="body1" sx={{ opacity: 0.8, maxWidth: 420 }}>
            Hesabını oluştur, e-postanı doğrula ve kampüsteki topluluklara üye ol.
          </Typography>
        </Box>

        {/* Telif satırı ortak AppFooter'a taşındı (PLAN-V3 §15.2) — burada tekrarlanmaz. */}
        <Box />
      </Box>

      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3, overflowY: 'auto' }}>
        <Box sx={{ width: '100%', maxWidth: 400, py: 4 }}>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 0.5 }}>
            Kayıt Ol
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Öğrenci hesabınızı oluşturun.
          </Typography>

          {successMessage ? (
            <Stack spacing={2}>
              <Alert severity="success">{successMessage}</Alert>
              <Button component={RouterLink} to="/login" variant="outlined">
                Giriş sayfasına dön
              </Button>
            </Stack>
          ) : (
            <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Controller
                name="email"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label="E-posta" type="email" autoComplete="username" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              <Controller
                name="password"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label="Parola" type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              <Controller
                name="studentNumber"
                control={control}
                render={({ field, fieldState }) => (
                  <TextField {...field} label="Öğrenci Numarası" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
                )}
              />

              <Controller
                name="departmentId"
                control={control}
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
                    {(departmentsQuery.data ?? []).map((dept) => (
                      <MenuItem key={dept.id} value={dept.id}>
                        {dept.name} ({dept.facultyName})
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />

              <Controller
                name="enrollmentYear"
                control={control}
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

              {serverError && <Alert severity="error">{serverError}</Alert>}

              <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
                Kayıt Ol
              </Button>

              <Button component={RouterLink} to="/login" variant="text" size="small">
                Zaten hesabınız var mı? Giriş yapın
              </Button>
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  )
}
