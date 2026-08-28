import { Button } from '@mui/material'
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import { Link as RouterLink } from 'react-router-dom'

interface BackButtonProps {
  /** Gidilecek rota — geçmişe bakılmaz, her zaman buraya gider. */
  to: string
  label?: string
}

export function BackButton({ to, label = 'Geri dön' }: BackButtonProps) {
  return (
    <Button
      component={RouterLink}
      to={to}
      variant="text"
      color="inherit"
      size="small"
      startIcon={<ArrowBackRoundedIcon />}
      sx={{
        cursor: 'pointer',
        alignSelf: 'flex-start',
        color: 'text.secondary',
        fontWeight: 700,
        px: 0.5,
        '&:hover': { color: 'primary.dark', bgcolor: 'transparent' },
      }}
    >
      {label}
    </Button>
  )
}
