import { Box, Stack, Typography, alpha } from '@mui/material'
import type { ReactNode } from 'react'
import logo from '../../assets/logo.png'
import { useLocale } from '../../i18n/LocaleContext'
import { brand } from '../../theme/tokens'
import { BrandMark } from './BrandMark'

interface AuthBrandPanelProps {
  title: string
  subtitle: string
  children?: ReactNode
}

export function AuthBrandPanel({ title, subtitle, children }: AuthBrandPanelProps) {
  const { t } = useLocale()
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
        background: `linear-gradient(165deg, ${brand.navy} 0%, ${brand.navy} 52%, ${alpha(brand.gold, 0.28)} 82%, ${alpha(brand.orange, 0.34)} 100%)`,
        '&::after': {
          content: '""',
          position: 'absolute',
          top: 0,
          right: 0,
          width: 5,
          height: '100%',
          background: `linear-gradient(180deg, ${brand.gold} 0%, ${brand.orange} 50%, ${brand.gold} 100%)`,
        },
      }}
    >
      <Box
        component="img"
        src={logo}
        alt=""
        aria-hidden
        sx={{
          position: 'absolute',
          right: { md: -56, lg: -28 },
          bottom: { md: -40, lg: -20 },
          width: { md: 320, lg: 400 },
          maxWidth: '88%',
          opacity: 0.14,
          pointerEvents: 'none',
          userSelect: 'none',
          filter: (theme) =>
            `drop-shadow(0 24px 40px ${alpha(theme.palette.common.black, 0.5)}) drop-shadow(0 0 28px ${alpha(brand.gold, 0.35)})`,
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
          background: (theme) =>
            `radial-gradient(ellipse at 14% 10%, ${alpha(brand.gold, 0.26)} 0%, transparent 40%), radial-gradient(ellipse at 90% 88%, ${alpha(brand.orange, 0.3)} 0%, transparent 46%), linear-gradient(180deg, ${alpha(theme.palette.common.black, 0.16)} 0%, transparent 34%)`,
        }}
      />

      <Box sx={{ position: 'relative', zIndex: 1 }}>
        <BrandMark light size="md" to="/" />
      </Box>

      <Box sx={{ position: 'relative', zIndex: 1, maxWidth: 440 }}>
        <Typography
          variant="overline"
          sx={{
            color: brand.gold,
            mb: 1,
            display: 'block',
            letterSpacing: 1.6,
            fontWeight: 800,
          }}
        >
          {t('brand.campusLife')}
        </Typography>
        <Box
          sx={{
            width: 56,
            height: 3,
            borderRadius: 2,
            mb: 2.5,
            background: `linear-gradient(90deg, ${brand.gold}, ${brand.orange})`,
          }}
        />
        <Typography variant="h3" sx={{ fontWeight: 800, mb: 2, fontSize: { md: 36, lg: 42 }, letterSpacing: '-0.03em' }}>
          {title}
        </Typography>
        <Typography variant="body1" sx={{ opacity: 0.88, maxWidth: 420, lineHeight: 1.75 }}>
          {subtitle}
        </Typography>
        {children}
      </Box>

      <Stack spacing={0.5} sx={{ position: 'relative', zIndex: 1 }}>
        <Typography variant="caption" sx={{ opacity: 0.8, fontWeight: 700 }}>
          {t('brand.university')}
        </Typography>
        <Typography variant="caption" sx={{ opacity: 0.6 }}>
          {t('brand.office')}
        </Typography>
      </Stack>
    </Box>
  )
}
