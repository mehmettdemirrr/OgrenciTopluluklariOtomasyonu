import { Box, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import logo from '../../assets/logo.png'
import { useLocale } from '../../i18n/LocaleContext'

interface BrandMarkProps {
  light?: boolean
  to?: string
  showSubtitle?: boolean
  size?: 'sm' | 'md'
}

export function BrandMark({ light = false, to, showSubtitle = false, size = 'sm' }: BrandMarkProps) {
  const { t } = useLocale()
  const logoSize = size === 'md' ? 40 : 32

  return (
    <Stack
      direction="row"
      spacing={1.25}
      {...(to ? { component: RouterLink, to } : {})}
      sx={{
        alignItems: 'center',
        minWidth: 0,
        color: 'inherit',
        textDecoration: 'none',
        cursor: to ? 'pointer' : 'default',
      }}
    >
      <Box component="img" src={logo} alt="" sx={{ width: logoSize, height: logoSize, flexShrink: 0 }} />
      <Box sx={{ minWidth: 0 }}>
        <Typography
          variant={size === 'md' ? 'h6' : 'subtitle1'}
          noWrap
          sx={{
            fontWeight: 800,
            color: light ? 'common.white' : 'text.primary',
            lineHeight: 1.2,
            letterSpacing: '-0.02em',
          }}
        >
          {t('brand.name')}
        </Typography>
        {showSubtitle && (
          <Typography
            variant="caption"
            noWrap
            sx={{
              display: { xs: 'none', sm: 'block' },
              color: light ? (theme) => theme.palette.common.white : 'text.secondary',
              opacity: light ? 0.72 : 1,
              lineHeight: 1.2,
            }}
          >
            {t('brand.university')}
          </Typography>
        )}
      </Box>
    </Stack>
  )
}
