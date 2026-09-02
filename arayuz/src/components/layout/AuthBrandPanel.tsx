import CheckRoundedIcon from '@mui/icons-material/CheckRounded'
import { Box, Chip, Stack, Typography, alpha } from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import logo from '../../assets/logo.png'
import { useLocale } from '../../i18n/LocaleContext'
import { brand, surfaces } from '../../theme/tokens'

interface AuthBrandPanelProps {
  title: string
  subtitle: string
  children?: ReactNode
}

export function AuthBrandPanel({ title, subtitle, children }: AuthBrandPanelProps) {
  const { t } = useLocale()
  const features = [t('auth.featureSecure'), t('auth.featurePortal'), t('auth.featureFast')]

  return (
    <Box
      sx={{
        display: { xs: 'none', md: 'flex' },
        flexDirection: 'column',
        justifyContent: 'space-between',
        width: { md: '46%' },
        position: 'relative',
        overflow: 'hidden',
        zIndex: 1,
        p: { md: 6, lg: 8 },
        bgcolor: surfaces.light.paper,
        backgroundImage: `radial-gradient(ellipse at 50% 42%, ${alpha(brand.navy, 0.04)} 0%, transparent 58%)`,
        borderRight: `1px solid ${alpha(brand.navy, 0.12)}`,
        boxShadow: `8px 0 28px ${alpha(brand.navy, 0.14)}`,
      }}
    >
      <Box
        component="img"
        src={logo}
        alt=""
        aria-hidden
        sx={{
          position: 'absolute',
          left: '50%',
          top: '46%',
          transform: 'translate(-50%, -50%)',
          width: { md: 360, lg: 460 },
          maxWidth: '78%',
          opacity: 0.08,
          pointerEvents: 'none',
          userSelect: 'none',
          filter: `drop-shadow(0 28px 48px ${alpha(brand.navy, 0.28)}) drop-shadow(0 8px 18px ${alpha(brand.navy, 0.12)})`,
        }}
      />

      <Box
        component={RouterLink}
        to="/"
        sx={{ position: 'relative', zIndex: 1, width: 72, height: 72, flexShrink: 0 }}
      >
        <Box component="img" src={logo} alt={t('brand.university')} sx={{ width: '100%', height: '100%' }} />
      </Box>

      <Box sx={{ position: 'relative', zIndex: 1, maxWidth: 480, py: 4 }}>
        <Typography
          variant="overline"
          sx={{
            color: brand.gold,
            mb: 1.5,
            display: 'block',
            letterSpacing: 1.8,
            fontWeight: 800,
          }}
        >
          {t('brand.digitalCampus')}
        </Typography>
        <Typography
          variant="h3"
          sx={{
            fontWeight: 800,
            mb: 2,
            fontSize: { md: 34, lg: 42 },
            letterSpacing: '-0.03em',
            color: brand.navy,
            lineHeight: 1.15,
          }}
        >
          {title}
        </Typography>
        <Typography variant="body1" sx={{ color: brand.greyDark, maxWidth: 440, lineHeight: 1.75 }}>
          {subtitle}
        </Typography>
        {children}
      </Box>

      <Stack
        direction="row"
        spacing={1.25}
        sx={{ position: 'relative', zIndex: 1, flexWrap: 'wrap', rowGap: 1.25 }}
      >
        {features.map((label) => (
          <Chip
            key={label}
            icon={<CheckRoundedIcon sx={{ fontSize: 16 }} />}
            label={label}
            sx={{
              height: 36,
              px: 0.5,
              bgcolor: surfaces.light.paper,
              color: brand.navy,
              fontWeight: 700,
              border: '1px solid',
              borderColor: alpha(brand.grey, 0.35),
              boxShadow: `0 6px 16px ${alpha(brand.navy, 0.08)}`,
              '& .MuiChip-icon': { color: brand.navy },
              '& .MuiChip-label': { px: 1 },
            }}
          />
        ))}
      </Stack>
    </Box>
  )
}
