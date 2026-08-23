import { Box, Typography } from '@mui/material'

/**
 * docs/PLAN-V3.md §15.2 (A-47): kurumsal alt bilgi — tek metin, tek yer.
 * Hem `AppShell` (giriş yapılmış sayfalar) hem `PublicLayout` (vitrin) hem de kabuk dışı
 * kimlik sayfaları (login/register/...) buradan besleniyor; metin başka hiçbir yerde tekrarlanmaz.
 * Y-56: renkler tema üzerinden, hex yazılmaz. Yıl her render'da yeniden hesaplanır (sabit değil).
 */
export function AppFooter() {
  return (
    <Box
      component="footer"
      sx={{
        py: 2.5,
        px: 2,
        mt: 'auto',
        borderTop: '1px solid',
        borderColor: 'divider',
        textAlign: 'center',
      }}
    >
      <Typography variant="caption" color="text.secondary">
        © {new Date().getFullYear()} Malatya Turgut Özal Üniversitesi Dijital Dönüşüm Koordinatörlüğü. Tüm Hakları Saklıdır
      </Typography>
    </Box>
  )
}
