import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'

const forgotPasswordSchema = z.object({
  email: z.string().min(1, 'E-posta gerekli.').email('Geçerli bir e-posta girin.'),
})

type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>

export function ForgotPasswordPage() {
  const [serverError, setServerError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<ForgotPasswordFormValues>({ resolver: zodResolver(forgotPasswordSchema), defaultValues: { email: '' } })

  const onSubmit = async (values: ForgotPasswordFormValues) => {
    setServerError(null)
    try {
      const response = await apiClient.post<{ message?: string }>('/auth/forgot-password', values)
      setSuccessMessage(response.data.message ?? 'E-postanız sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderildi.')
    } catch (error) {
      setServerError(extractErrorMessage(error, 'İşlem gerçekleştirilemedi.'))
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3 }}>
      <Box sx={{ width: '100%', maxWidth: 380 }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 0.5 }}>
          Parolamı Unuttum
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          E-posta adresinize bir sıfırlama bağlantısı gönderelim.
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

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
              Sıfırlama Bağlantısı Gönder
            </Button>

            <Button component={RouterLink} to="/login" variant="text" size="small">
              Giriş sayfasına dön
            </Button>
          </Box>
        )}
      </Box>
    </Box>
  )
}
