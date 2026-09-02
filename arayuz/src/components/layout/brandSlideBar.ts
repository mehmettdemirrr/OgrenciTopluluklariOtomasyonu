import { alpha } from '@mui/material'
import { keyframes, type SxProps, type Theme } from '@mui/material/styles'
import { brand } from '../../theme/tokens'

export const brandBarSlide = keyframes`
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
`

export const brandSlideBarSx: SxProps<Theme> = {
  color: 'common.white',
  bgcolor: 'transparent',
  backgroundImage: `linear-gradient(90deg, ${brand.navy} 0%, ${brand.orange} 38%, ${brand.gold} 68%, ${brand.navy} 100%)`,
  backgroundSize: '260% 100%',
  animation: `${brandBarSlide} 14s ease-in-out infinite`,
  boxShadow: (theme) => `0 8px 24px ${alpha(theme.palette.common.black, 0.18)}`,
  '@media (prefers-reduced-motion: reduce)': {
    animation: 'none',
    backgroundPosition: '20% 50%',
  },
}

export const brandSlideToolbarSx: SxProps<Theme> = {
  gap: 1,
  minHeight: 72,
  color: 'common.white',
  '& .MuiIconButton-root': { color: 'common.white' },
  '& .MuiToggleButtonGroup-root': {
    bgcolor: (theme) => alpha(theme.palette.common.white, 0.12),
  },
  '& .MuiToggleButton-root': {
    color: 'common.white',
    borderColor: (theme) => alpha(theme.palette.common.white, 0.28),
    '&.Mui-selected': {
      color: 'common.white',
      bgcolor: (theme) => alpha(theme.palette.common.white, 0.22),
    },
  },
}
