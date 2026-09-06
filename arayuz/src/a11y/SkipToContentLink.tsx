import { Box } from '@mui/material'
import { useLocale } from '../i18n/LocaleContext'

/** docs/MIMARI.md · K-48: klavye kullanıcısı menüyü atlayıp doğrudan içeriğe gider. */
export function SkipToContentLink() {
  const { t } = useLocale()

  return (
    <Box
      component="a"
      href="#main-content"
      sx={{
        position: 'absolute',
        left: 8,
        top: -64,
        zIndex: (theme) => theme.zIndex.tooltip + 1,
        px: 2,
        py: 1,
        borderRadius: 2,
        bgcolor: 'primary.dark',
        color: 'common.white',
        textDecoration: 'none',
        '&:focus-visible': { top: 8 },
      }}
    >
      {t('a11y.skipToContent')}
    </Box>
  )
}
