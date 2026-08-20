import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Paper, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'

// Y-35: yalnızca biçim doğrulanır — "bu e-posta var mı" gibi iş kuralı kararları API'de verilir.
const loginSchema = z.object({
  email: z.string().min(1, 'E-posta gerekli.').email('Geçerli bir e-posta girin.'),
  password: z.string().min(1, 'Parola gerekli.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
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
      navigate('/clubs', { replace: true })
    } catch (error) {
      setServerError(extractErrorMessage(error, 'Giriş başarısız. Bilgilerinizi kontrol edin.'))
    }
  }

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        bgcolor: 'grey.100',
      }}
    >
      <Paper elevation={3} sx={{ p: 4, width: 360 }}>
        <Typography variant="h5" component="h1" gutterBottom>
          Öğrenci Toplulukları Otomasyonu
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Devam etmek için giriş yapın.
        </Typography>

        <Box
          component="form"
          onSubmit={handleSubmit(onSubmit)}
          sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}
        >
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
        </Box>
      </Paper>
    </Box>
  )
}
