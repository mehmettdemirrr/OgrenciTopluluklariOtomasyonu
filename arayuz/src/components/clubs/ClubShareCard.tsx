import { Box, Button, Card, CardContent, Dialog, DialogContent, Stack, Typography, alpha } from '@mui/material'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { useLocale } from '../../i18n/LocaleContext'
import { useNotifier } from '../../notifications/NotifierProvider'

/** docs/MIMARI.md · K-51: paylaşım kartı — QR istemcide üretilir, sunucuya uç eklenmez. */
export function ClubShareCard({ clubName, clubId }: { clubName: string; clubId: number }) {
  const { t } = useLocale()
  const notify = useNotifier()
  const [open, setOpen] = useState(false)
  const [dataUrl, setDataUrl] = useState<string | null>(null)
  const url = `${window.location.origin}/kulupler/${clubId}`

  useEffect(() => {
    if (!open) {
      return
    }
    QRCode.toDataURL(url, { width: 320, margin: 1 })
      .then(setDataUrl)
      .catch(() => setDataUrl(null))
  }, [open, url])

  const copyLink = async () => {
    await navigator.clipboard.writeText(url).catch(() => undefined)
    notify({ message: t('club.shareCopied'), severity: 'success' })
  }

  return (
    <>
      <Card
        variant="outlined"
        sx={{
          borderRadius: 3,
          color: 'common.white',
          background: (theme) => `linear-gradient(135deg, ${theme.palette.secondary.main} 0%, ${alpha(theme.palette.secondary.dark ?? theme.palette.secondary.main, 0.9)} 100%)`,
        }}
      >
        <CardContent>
          <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center' }}>
            <QrCode2OutlinedIcon sx={{ fontSize: 56, opacity: 0.9 }} />
            <Typography variant="body2">{t('club.shareTitle')}</Typography>
            <Stack direction="row" spacing={1}>
              <Button size="small" variant="contained" color="inherit" sx={{ color: 'secondary.main' }} onClick={() => setOpen(true)}>
                {t('club.shareShow')}
              </Button>
              <Button size="small" variant="outlined" color="inherit" onClick={copyLink}>
                {t('club.shareCopy')}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs">
        <DialogContent sx={{ textAlign: 'center' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2 }}>
            {clubName}
          </Typography>
          {dataUrl && <Box component="img" src={dataUrl} alt={t('club.qrAlt')} sx={{ width: '100%', maxWidth: 280 }} />}
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1, wordBreak: 'break-all' }}>
            {url}
          </Typography>
        </DialogContent>
      </Dialog>
    </>
  )
}
