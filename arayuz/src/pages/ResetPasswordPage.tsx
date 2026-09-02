import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/

type ResetPasswordFormValues = { newPassword: string }

export function ResetPasswordPage() {
  const { t } = useLocale()
  useDocumentTitle(t('auth.resetDocTitle'))

  const resetPasswordSchema = z.object({
    newPassword: z.string().regex(strongPasswordRegex, t('validation.passwordStrong')),
  })

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
      setServerError(t('auth.linkInvalid'))
      return
    }

    setServerError(null)
    try {
      const response = await apiClient.post<{ message?: string }>('/auth/reset-password', {
        userId: Number(userId), token, newPassword: values.newPassword,
      })
      setSuccessMessage(response.data.message ?? t('auth.resetOk'))
    } catch (error) {
      setServerError(extractErrorMessage(error, t('auth.resetFail')))
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
          {t('auth.resetTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          {t('auth.resetLead')}
        </Typography>

        {successMessage ? (
          <Stack spacing={2}>
            <Alert severity="success">{successMessage}</Alert>
            <Button component={RouterLink} to="/login" variant="contained">
              {t('common.login')}
            </Button>
          </Stack>
        ) : !userId || !token ? (
          <Alert severity="error">{t('auth.linkInvalidLong')}</Alert>
        ) : (
          <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Controller
              name="newPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label={t('auth.newPassword')} type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large">
              {t('auth.updatePassword')}
            </Button>
          </Box>
        )}
      </AuthFormCard>
    </Box>
  )
}
