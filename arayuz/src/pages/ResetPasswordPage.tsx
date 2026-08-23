import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'

const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/

const resetPasswordSchema = z.object({
  newPassword: z.string().regex(strongPasswordRegex, 'Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.'),
})

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [serverError, setServerError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const {
    control,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<ResetPasswordFormValues>({ resolver: zodResolver(resetPasswordSchema), defaultValues: { newPassword: '' } })

  const onSubmit = async (values: ResetPasswordFormValues) => {
    if (!userId || !token) {
      setServerError('Bağlantı eksik veya geçersiz.')
      return
    }

    setServerError(null)
    try {
      const response = await apiClient.post<{ message?: string }>('/auth/reset-password', {
        userId: Number(userId), token, newPassword: values.newPassword,
      })
      setSuccessMessage(response.data.message ?? 'Parolanız güncellendi.')
    } catch (error) {
      setServerError(extractErrorMessage(error, 'Parola sıfırlanamadı.'))
    }
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: 3 }}>
      <Box sx={{ width: '100%', maxWidth: 380 }}>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 700, mb: 0.5 }}>
          Yeni Parola Belirle
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Hesabınız için yeni bir parola girin.
        </Typography>

        {successMessage ? (
          <Stack spacing={2}>
            <Alert severity="success">{successMessage}</Alert>
            <Button component={RouterLink} to="/login" variant="contained">
              Giriş Yap
            </Button>
          </Stack>
        ) : !userId || !token ? (
          <Alert severity="error">Bağlantı eksik veya geçersiz. E-postanızdaki bağlantıyı tekrar kullanmayı deneyin.</Alert>
        ) : (
          <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Controller
              name="newPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label="Yeni Parola" type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
              Parolayı Güncelle
            </Button>
          </Box>
        )}
      </Box>
    </Box>
  )
}
