import { Box, Button } from '@mui/material'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'

const CAMPUS_HASH = 'kampusler'

export function CampusLocationMenu() {
  const { t } = useLocale()

  return (
    <Button
      color="inherit"
      component={RouterLink}
      to={`/#${CAMPUS_HASH}`}
      startIcon={<PlaceOutlinedIcon />}
      aria-label={t('home.campuses')}
      onClick={() => {
        window.setTimeout(() => {
          document.getElementById(CAMPUS_HASH)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
        }, 0)
      }}
      sx={{
        fontWeight: 700,
        color: 'inherit',
        minWidth: { xs: 40, sm: 'auto' },
        px: { xs: 1, sm: 1.5 },
        '& .MuiButton-startIcon': { mr: { xs: 0, sm: 1 } },
      }}
    >
      <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>
        {t('home.campuses')}
      </Box>
    </Button>
  )
}
