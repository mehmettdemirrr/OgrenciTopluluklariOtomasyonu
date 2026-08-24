import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import logo from '../assets/logo.png'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

// Y-35: yalnızca biçim doğrulanır — "bu e-posta var mı" gibi iş kuralı kararları API'de verilir.
const loginSchema = z.object({
  email: z.string().min(1, 'E-posta gerekli.').email('Geçerli bir e-posta girin.'),
  password: z.string().min(1, 'Parola gerekli.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  useDocumentTitle('Giriş Yap')

  const { login } = useAuth()
  const navigate = useNavigate()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = async (values: LoginFormValues) => {
    setServerError(null)
    try {
      await login(values.email, values.password)
      navigate('/panel', { replace: true })
    } catch (error) {
      setServerError(extractErrorMessage(error, 'Giriş başarısız. Bilgilerinizi kontrol edin.'))
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
            Kampüsteki topluluklar, tek yerde.
          </Typography>
          <Typography variant="body1" sx={{ opacity: 0.8, maxWidth: 420 }}>
            Üyelik başvurusundan etkinlik onayına, yetki yönetiminden raporlamaya kadar tüm topluluk
            süreçlerini buradan yönetin.
          </Typography>
        </Box>

        {/* Telif satırı ortak AppFooter'a taşındı (PLAN-V3 §15.2) — burada tekrarlanmaz. */}
        <Box />
      </Box>

      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3 }}>
        <Box sx={{ width: '100%', maxWidth: 360 }}>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 0.5 }}>
            Giriş Yap
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Devam etmek için hesabınıza giriş yapın.
          </Typography>

          <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Controller
              name="email"
              control={control}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  label="E-posta"
                  type="email"
                  autoComplete="username"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  fullWidth
                />
              )}
            />

            <Controller
              name="password"
              control={control}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  label="Parola"
                  type="password"
                  autoComplete="current-password"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  fullWidth
                />
              )}
            />

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
              Giriş Yap
            </Button>

            <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
              <Button component={RouterLink} to="/register" variant="text" size="small">
                Hesabınız yok mu? Kayıt olun
              </Button>
              <Button component={RouterLink} to="/forgot-password" variant="text" size="small">
                Parolamı unuttum
              </Button>
            </Stack>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
