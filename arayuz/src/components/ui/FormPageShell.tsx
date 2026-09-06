import { Box, Button, Card, CardContent, Stack } from '@mui/material'
import { useEffect, useState, type ReactNode } from 'react'
import { ConfirmDialog } from './ConfirmDialog'
import { PageHeader } from './PageHeader'
import { useLocale } from '../../i18n/LocaleContext'

interface FormPageShellProps {
  title: string
  description?: string
  backTo: string
  isDirty: boolean
  isSubmitting: boolean
  submitLabel: string
  onSubmit: () => void
  onCancel: () => void
  children: ReactNode
}

/** docs/MIMARI.md · K-47: form sayfalarının ortak kabuğu — başlık, kart, kaydet/vazgeç ve çıkış uyarısı. */
export function FormPageShell({
  title, description, backTo, isDirty, isSubmitting, submitLabel, onSubmit, onCancel, children,
}: FormPageShellProps) {
  const { t } = useLocale()
  const [confirmOpen, setConfirmOpen] = useState(false)

  // Sekme kapatma/yenileme için tarayıcı uyarısı; uygulama içi çıkış aşağıdaki onayla sorulur.
  useEffect(() => {
    if (!isDirty) {
      return
    }
    const handler = (event: BeforeUnloadEvent) => {
      event.preventDefault()
      event.returnValue = ''
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [isDirty])

  return (
    <>
      <PageHeader title={title} description={description} backTo={backTo} />

      <Card variant="outlined" sx={{ borderRadius: 3 }}>
        <CardContent sx={{ p: { xs: 2, md: 3 } }}>
          <Box sx={{ maxWidth: 760 }}>{children}</Box>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={1} sx={{ mt: 3, justifyContent: 'flex-end' }}>
        <Button onClick={() => (isDirty ? setConfirmOpen(true) : onCancel())}>{t('common.cancel')}</Button>
        <Button variant="contained" size="large" onClick={onSubmit} disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </Stack>

      <ConfirmDialog
        open={confirmOpen}
        title={t('form.discardTitle')}
        description={t('form.discardBody')}
        confirmLabel={t('form.discardConfirm')}
        destructive
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => {
          setConfirmOpen(false)
          onCancel()
        }}
      />
    </>
  )
}
