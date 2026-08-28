import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { AuthBrandPanel } from '../components/layout/AuthBrandPanel'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
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
      <AuthBrandPanel
        title="Kampüsteki topluluklar, tek yerde."
        subtitle="Üyelik başvurusundan etkinlik onayına, yetki yönetiminden raporlamaya kadar tüm topluluk süreçlerini buradan yönetin."
      />

      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: { xs: 2.5, md: 4 } }}>
        <AuthFormCard>
          <BackButton to="/" label="Anasayfaya dön" />
          <Box sx={{ display: { xs: 'block', md: 'none' }, mb: 3, mt: 1 }}>
            <BrandMark to="/" showSubtitle />
          </Box>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mb: 0.75, fontSize: 28 }}>
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

            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 0.5 }}>
              <Button component={RouterLink} to="/register" variant="text" size="small">
                Hesabınız yok mu? Kayıt olun
              </Button>
              <Button component={RouterLink} to="/forgot-password" variant="text" size="small">
                Parolamı unuttum
              </Button>
            </Stack>
          </Box>
        </AuthFormCard>
      </Box>
    </Box>
  )
}
