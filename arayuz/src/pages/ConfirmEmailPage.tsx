import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

type Status = 'confirming' | 'success' | 'error'

export function ConfirmEmailPage() {
  useDocumentTitle('E-posta Doğrulama')

  const [searchParams] = useSearchParams()
  const userId = searchParams.get('userId')
  const token = searchParams.get('token')
  const paramsMissing = !userId || !token

  const [status, setStatus] = useState<Status>(paramsMissing ? 'error' : 'confirming')
  const [message, setMessage] = useState<string>(paramsMissing ? 'Bağlantı eksik veya geçersiz.' : '')

  useEffect(() => {
    if (paramsMissing) {
      return
    }

    apiClient
      .post<{ message?: string }>('/auth/confirm-email', { userId: Number(userId), token })
      .then((response) => {
        setStatus('success')
        setMessage(response.data.message ?? 'E-posta adresiniz doğrulandı.')
      })
      .catch((error) => {
        setStatus('error')
        setMessage(extractErrorMessage(error, 'Doğrulama başarısız oldu.'))
      })
  }, [userId, token, paramsMissing])

  return (
    <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', p: { xs: 2.5, md: 4 } }}>
      <AuthFormCard>
        <Stack spacing={2} sx={{ textAlign: 'center', alignItems: 'center' }}>
          <BackButton to="/login" />
          <BrandMark to="/" showSubtitle />
          {status === 'confirming' && (
            <>
              <CircularProgress />
              <Typography variant="body2" color="text.secondary">
                E-posta adresiniz doğrulanıyor…
              </Typography>
            </>
          )}

          {status === 'success' && (
            <>
              <Alert severity="success" sx={{ width: '100%' }}>
                {message}
              </Alert>
              <Button component={RouterLink} to="/login" variant="contained">
                Giriş Yap
              </Button>
            </>
          )}

          {status === 'error' && (
            <>
              <Alert severity="error" sx={{ width: '100%' }}>
                {message}
              </Alert>
              <Button component={RouterLink} to="/login" variant="outlined">
                Giriş sayfasına dön
              </Button>
            </>
          )}
        </Stack>
      </AuthFormCard>
    </Box>
  )
}
