import { Box, Container, Stack, Typography } from '@mui/material'
import logo from '../../assets/logo.png'
import { useLocale } from '../../i18n/LocaleContext'

/**
 * docs/PLAN-V3.md §15.2 (A-47): kurumsal alt bilgi — tek metin, tek yer.
 * Hem `AppShell` (giriş yapılmış sayfalar) hem `PublicLayout` (vitrin) hem de kabuk dışı
 * kimlik sayfaları (login/register/...) buradan besleniyor; metin başka hiçbir yerde tekrarlanmaz.
 * Y-56: renkler tema üzerinden, hex yazılmaz. Yıl her render'da yeniden hesaplanır (sabit değil).
 */
export function AppFooter() {
  const { t } = useLocale()
  const year = new Date().getFullYear()

  return (
    <Box
      component="footer"
      sx={{
        mt: 'auto',
        py: { xs: 2.5, sm: 3 },
        borderTop: '1px solid',
        borderColor: 'divider',
        bgcolor: 'background.default',
      }}
    >
      <Container maxWidth="lg">
        <Stack direction="row" spacing={{ xs: 1.5, sm: 2 }} sx={{ alignItems: 'center', justifyContent: 'center' }}>
          <Box
            component="img"
            src={logo}
            alt=""
            sx={{ width: { xs: 56, sm: 72 }, height: { xs: 56, sm: 72 }, flexShrink: 0 }}
          />
          <Box sx={{ minWidth: 0 }}>
            <Typography
              variant="subtitle1"
              sx={{
                fontWeight: 800,
                color: 'text.primary',
                letterSpacing: '0.02em',
                textTransform: 'uppercase',
                lineHeight: 1.25,
              }}
            >
              {t('brand.university')}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25, lineHeight: 1.4 }}>
              {t('brand.footerCredit', { year })}
            </Typography>
          </Box>
        </Stack>
      </Container>
    </Box>
  )
}
