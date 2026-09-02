import { Box, Typography, alpha } from '@mui/material'
import { useLocale } from '../../i18n/LocaleContext'

/**
 * docs/PLAN-V3.md §15.2 (A-47): kurumsal alt bilgi — tek metin, tek yer.
 * Hem `AppShell` (giriş yapılmış sayfalar) hem `PublicLayout` (vitrin) hem de kabuk dışı
 * kimlik sayfaları (login/register/...) buradan besleniyor; metin başka hiçbir yerde tekrarlanmaz.
 * Y-56: renkler tema üzerinden, hex yazılmaz. Yıl her render'da yeniden hesaplanır (sabit değil).
 */
export function AppFooter() {
  const { t } = useLocale()
  return (
    <Box
      component="footer"
      sx={{
        py: 3,
        px: 2,
        mt: 'auto',
        borderTop: '1px solid',
        borderColor: 'divider',
        textAlign: 'center',
        bgcolor: (theme) => alpha(theme.palette.secondary.main, 0.03),
      }}
    >
      <Typography variant="body2" sx={{ fontWeight: 600,         color: 'text.primary', mb: 0.5 }}>
        {t('brand.university')}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        © {new Date().getFullYear()} {t('brand.office')}. {t('brand.rights')}
      </Typography>
    </Box>
  )
}
