import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Button, Divider, IconButton, InputAdornment, Stack, TextField, Typography, alpha } from '@mui/material'
import MailOutlineOutlinedIcon from '@mui/icons-material/MailOutlineOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import VisibilityOffOutlinedIcon from '@mui/icons-material/VisibilityOffOutlined'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { extractErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import universityLogo from '../assets/logo.png'
import { AuthBrandPanel } from '../components/layout/AuthBrandPanel'
import { BrandMark } from '../components/layout/BrandMark'
import { AuthFormCard } from '../components/ui/AuthFormCard'
import { BackButton } from '../components/ui/BackButton'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { useLocale } from '../i18n/LocaleContext'

type LoginFormValues = { email: string; password: string }

export function LoginPage() {
  const { t } = useLocale()
  useDocumentTitle(t('auth.loginTitle'))

  const loginSchema = z.object({
    email: z.string().min(1, t('validation.emailRequired')).email(t('validation.emailInvalid')),
    password: z.string().min(1, t('validation.passwordRequired')),
  })

  const { login } = useAuth()
  const navigate = useNavigate()
  const [serverError, setServerError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)

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
      setServerError(extractErrorMessage(error, t('auth.loginFail')))
    }
  }

  return (
    <Box sx={{ flex: 1, display: 'flex' }}>
      <AuthBrandPanel title={t('auth.panelTitle')} subtitle={t('auth.panelLead')} />

      <Box
        sx={{
          flex: 1,
          position: 'relative',
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: { xs: 2.5, md: 5 },
          bgcolor: 'common.white',
        }}
      >
        <Box
          component="img"
          src={universityLogo}
          alt=""
          aria-hidden
          sx={{
            position: 'absolute',
            left: '50%',
            top: '46%',
            transform: 'translate(-50%, -50%)',
            width: { xs: 280, md: 520 },
            maxWidth: '78%',
            opacity: 0.14,
            pointerEvents: 'none',
            userSelect: 'none',
            filter: (theme) =>
              `drop-shadow(0 28px 48px ${alpha(theme.palette.secondary.main, 0.42)}) drop-shadow(0 10px 20px ${alpha(theme.palette.common.black, 0.22)})`,
          }}
        />
        <Box sx={{ position: 'relative', zIndex: 1, width: '100%', display: 'flex', justifyContent: 'center' }}>
        <AuthFormCard>
          <BackButton to="/" label={t('common.backHome')} />
          <Box sx={{ display: { xs: 'block', md: 'none' }, mb: 3, mt: 1 }}>
            <BrandMark to="/" showSubtitle />
          </Box>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mt: 2.5, mb: 0.75, fontSize: { xs: 26, sm: 30 }, letterSpacing: '-0.03em' }}>
            {t('auth.loginTitle')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3.5, lineHeight: 1.6 }}>
            {t('auth.loginLead')}
          </Typography>

          <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Controller
              name="email"
              control={control}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  label={t('auth.email')}
                  type="email"
                  autoComplete="username"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  fullWidth
                  size="medium"
                  slotProps={{
                    input: {
                      startAdornment: (
                        <InputAdornment position="start">
                          <MailOutlineOutlinedIcon fontSize="small" color="action" />
                        </InputAdornment>
                      ),
                    },
                  }}
                />
              )}
            />

            <Controller
              name="password"
              control={control}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  label={t('auth.password')}
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  fullWidth
                  size="medium"
                  slotProps={{
                    input: {
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockOutlinedIcon fontSize="small" color="action" />
                        </InputAdornment>
                      ),
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label={showPassword ? t('auth.hidePassword') : t('auth.showPassword')}
                            onClick={() => setShowPassword((current) => !current)}
                            edge="end"
                            size="small"
                          >
                            {showPassword ? <VisibilityOffOutlinedIcon fontSize="small" /> : <VisibilityOutlinedIcon fontSize="small" />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    },
                  }}
                />
              )}
            />

            <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: -0.5 }}>
              <Button component={RouterLink} to="/forgot-password" variant="text" size="small" sx={{ fontWeight: 700 }}>
                {t('auth.forgot')}
              </Button>
            </Box>

            {serverError && <Alert severity="error">{serverError}</Alert>}

            <Button type="submit" variant="contained" disabled={isSubmitting} size="large" sx={{ py: 1.35, fontWeight: 800 }}>
              {t('auth.loginTitle')}
            </Button>

            <Divider sx={{ my: 0.5 }} />

            <Stack sx={{ alignItems: 'center' }}>
              <Button component={RouterLink} to="/register" variant="text" size="small" sx={{ fontWeight: 700 }}>
                {t('auth.noAccount')}
              </Button>
            </Stack>
          </Box>
        </AuthFormCard>
        </Box>
      </Box>
    </Box>
  )
}
