import AccessibilityNewRoundedIcon from '@mui/icons-material/AccessibilityNewRounded'
import { Fab, Tooltip } from '@mui/material'
import { useState } from 'react'
import { createPortal } from 'react-dom'
import { useLocale } from '../i18n/LocaleContext'
import { useAccessibility } from './AccessibilityContext'
import { AccessibilityMenu } from './AccessibilityMenu'
import { AccessibilityOverlays } from './AccessibilityOverlays'

/** docs/MIMARI.md · K-48: her sayfada duran erişilebilirlik paneli (MDBF widget konumu). */
export function AccessibilityFab() {
  const { t } = useLocale()
  const { prefs } = useAccessibility()
  const [open, setOpen] = useState(false)
  const side = prefs.panelSide

  return createPortal(
    <>
      <Tooltip title={t('a11y.open')} placement={side === 'left' ? 'right' : 'left'}>
        <Fab
          color="primary"
          aria-label={t('a11y.open')}
          aria-expanded={open}
          onClick={() => setOpen((current) => !current)}
          sx={{
            position: 'fixed',
            [side]: 20,
            bottom: 20,
            width: 60,
            height: 60,
            zIndex: (theme) => theme.zIndex.modal + 1,
            boxShadow: (theme) => theme.shadows[8],
          }}
        >
          <AccessibilityNewRoundedIcon sx={{ fontSize: 28 }} />
        </Fab>
      </Tooltip>

      {open && <AccessibilityMenu onClose={() => setOpen(false)} />}
      <AccessibilityOverlays />
    </>,
    document.body,
  )
}
