import { Box, Paper, Stack, Typography, alpha } from '@mui/material'
import type { ReactNode } from 'react'
import type { SxProps, Theme } from '@mui/material/styles'

interface DetailHeroProps {
  media?: ReactNode
  chips?: ReactNode
  title?: string
  titleComponent?: 'h1' | 'h2'
  subtitle?: string
  description?: string | null
  emptyDescription?: string
  actions?: ReactNode
  children?: ReactNode
  sx?: SxProps<Theme>
}

export function DetailHero({
  media,
  chips,
  title,
  titleComponent = 'h2',
  subtitle,
  description,
  emptyDescription,
  actions,
  children,
  sx,
}: DetailHeroProps) {
  const descriptionText = description?.trim() || emptyDescription

  return (
    <Paper
      variant="outlined"
      sx={{
        p: { xs: 2.5, md: 3.5 },
        borderRadius: 3,
        overflow: 'hidden',
        position: 'relative',
        background: (theme) =>
          `linear-gradient(125deg, ${alpha(theme.palette.secondary.main, 0.05)} 0%, ${alpha(theme.palette.primary.main, 0.1)} 52%, ${alpha(theme.palette.warning.main, 0.07)} 100%)`,
        ...sx,
      }}
    >
      <Stack spacing={3}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} sx={{ alignItems: { sm: 'flex-start' } }}>
          {media}
          <Stack spacing={1.25} sx={{ flex: 1, minWidth: 0 }}>
            {chips && (
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {chips}
              </Stack>
            )}
            {title && (
              <Typography variant="h4" component={titleComponent} sx={{ fontWeight: 800, fontSize: { xs: 26, md: 32 } }}>
                {title}
              </Typography>
            )}
            {subtitle && (
              <Typography variant="subtitle2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
            {descriptionText && (
              <Typography
                variant="body1"
                color="text.secondary"
                sx={{ lineHeight: 1.75, maxWidth: 720, fontStyle: description?.trim() ? 'normal' : 'italic' }}
              >
                {descriptionText}
              </Typography>
            )}
            {actions && (
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', pt: 0.5 }}>
                {actions}
              </Stack>
            )}
          </Stack>
        </Stack>
        {children}
      </Stack>
    </Paper>
  )
}

export function DetailMedia({
  src,
  alt = '',
  fallback,
}: {
  src?: string | null
  alt?: string
  fallback: ReactNode
}) {
  return (
    <Box
      sx={{
        width: { xs: 96, md: 120 },
        height: { xs: 96, md: 120 },
        flexShrink: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.paper',
        borderRadius: 3,
        border: '1px solid',
        borderColor: 'divider',
        overflow: 'hidden',
        boxShadow: (theme) => `0 10px 24px ${alpha(theme.palette.secondary.main, 0.08)}`,
      }}
    >
      {src ? (
        <Box component="img" src={src} alt={alt} sx={{ width: '100%', height: '100%', objectFit: 'contain', p: 1.5 }} />
      ) : (
        fallback
      )}
    </Box>
  )
}
