import { Box, Stack, Typography, alpha } from '@mui/material'
import type { ReactNode } from 'react'
import { BrandMark } from './BrandMark'

interface AuthBrandPanelProps {
  title: string
  subtitle: string
  children?: ReactNode
}

export function AuthBrandPanel({ title, subtitle, children }: AuthBrandPanelProps) {
  return (
    <Box
      sx={{
        display: { xs: 'none', md: 'flex' },
        flexDirection: 'column',
        justifyContent: 'space-between',
        width: { md: '44%' },
        position: 'relative',
        overflow: 'hidden',
        color: 'common.white',
        p: { md: 6, lg: 8 },
        background: (theme) =>
          `linear-gradient(165deg, ${theme.palette.secondary.main} 0%, ${theme.palette.primary.dark} 72%, ${theme.palette.secondary.main} 100%)`,
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          width: 320,
          height: 320,
          borderRadius: '50%',
          top: -80,
          right: -60,
          bgcolor: (theme) => alpha(theme.palette.common.white, 0.06),
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          width: 220,
          height: 220,
          borderRadius: '50%',
          bottom: 80,
          left: -70,
          bgcolor: (theme) => alpha(theme.palette.common.white, 0.05),
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          width: 12,
          height: 64,
          borderRadius: 8,
          top: 120,
          right: 48,
          bgcolor: 'accent.main',
          opacity: 0.9,
        }}
      />

      <BrandMark light size="md" to="/" />

      <Box sx={{ position: 'relative', zIndex: 1, maxWidth: 440 }}>
        <Typography variant="overline" sx={{ color: 'accent.main', mb: 1.5, display: 'block' }}>
          Kampüs yaşamı
        </Typography>
        <Typography variant="h3" sx={{ fontWeight: 800, mb: 2, fontSize: { md: 36, lg: 42 } }}>
          {title}
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.82, maxWidth: 420, lineHeight: 1.7 }}>
          {subtitle}
        </Typography>
        {children}
      </Box>

      <Stack spacing={0.5} sx={{ position: 'relative', zIndex: 1 }}>
        <Typography variant="caption" sx={{ opacity: 0.7 }}>
          Malatya Turgut Özal Üniversitesi
        </Typography>
        <Typography variant="caption" sx={{ opacity: 0.55 }}>
          Dijital Dönüşüm Koordinatörlüğü
        </Typography>
      </Stack>
    </Box>
  )
}
