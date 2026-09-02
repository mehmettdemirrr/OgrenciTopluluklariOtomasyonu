import { Button } from '@mui/material'
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'

interface BackButtonProps {
  /** Gidilecek rota — geçmişe bakılmaz, her zaman buraya gider. */
  to: string
  label?: string
}

export function BackButton({ to, label }: BackButtonProps) {
  const { t } = useLocale()
  const text = label ?? t('common.back')
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
      {text}
    </Button>
  )
}
