import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

type ForgotPasswordFormValues = { email: string }

export function ForgotPasswordPage() {
  const { t } = useLocale()
  useDocumentTitle(t('auth.forgotTitle'))

  const forgotPasswordSchema = z.object({
    email: z.string().min(1, t('validation.emailRequired')).email(t('validation.emailInvalid')),
  })

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
      setSuccessMessage(response.data.message ?? t('auth.forgotOk'))
    } catch (error) {
      setServerError(extractErrorMessage(error, t('auth.forgotFail')))
    }
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: { xs: 2.5, md: 4 } }}>
      <AuthFormCard>
        <BackButton to="/login" />
        <Box sx={{ mb: 3, mt: 1 }}>
          <BrandMark to="/" showSubtitle />
        </Box>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mb: 0.75, fontSize: 28 }}>
          {t('auth.forgotTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          {t('auth.forgotLead')}
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
            <Controller
              name="email"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label={t('auth.email')} type="email" autoComplete="username" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
              {t('auth.forgotSend')}
            </Button>

            <Button component={RouterLink} to="/login" variant="text" size="small">
              {t('common.backLogin')}
            </Button>
          </Box>
        )}
      </AuthFormCard>
    </Box>
  )
}
