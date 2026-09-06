import { Box, Button, Divider, Fab, FormControlLabel, Popover, Stack, Switch, ToggleButton, ToggleButtonGroup, Tooltip, Typography } from '@mui/material'
import AccessibilityNewRoundedIcon from '@mui/icons-material/AccessibilityNewRounded'
import { useState } from 'react'
import { useAccessibility } from './AccessibilityContext'
import { useLocale } from '../i18n/LocaleContext'

/** docs/MIMARI.md · K-48: her sayfada sol altta duran erişilebilirlik paneli. */
export function AccessibilityFab() {
  const { t } = useLocale()
  const { prefs, setPref, reset } = useAccessibility()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)

  return (
    <>
      <Tooltip title={t('a11y.open')} placement="right">
        <Fab
          color="primary"
          size="medium"
          aria-label={t('a11y.open')}
          onClick={(event) => setAnchor(event.currentTarget)}
          sx={{ position: 'fixed', left: 16, bottom: 16, zIndex: (theme) => theme.zIndex.drawer + 2 }}
        >
          <AccessibilityNewRoundedIcon />
        </Fab>
      </Tooltip>

      <Popover
        open={Boolean(anchor)}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'top', horizontal: 'left' }}
        transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        slotProps={{ paper: { sx: { p: 2, width: 280, borderRadius: 3 } } }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 800, mb: 1.5 }}>
          {t('a11y.title')}
        </Typography>

        <Typography variant="caption" color="text.secondary">
          {t('a11y.fontSize')}
        </Typography>
        <ToggleButtonGroup
          exclusive
          fullWidth
          size="small"
          value={prefs.fontScale}
          onChange={(_, value: number | null) => {
            if (value) setPref('fontScale', value)
          }}
          aria-label={t('a11y.fontSize')}
          sx={{ mb: 1.5, mt: 0.5 }}
        >
          <ToggleButton value={1}>A</ToggleButton>
          <ToggleButton value={1.15} sx={{ fontSize: '1.05rem' }}>A</ToggleButton>
          <ToggleButton value={1.3} sx={{ fontSize: '1.2rem' }}>A</ToggleButton>
        </ToggleButtonGroup>

        <Divider sx={{ mb: 1 }} />

        <Stack>
          <FormControlLabel
            control={<Switch checked={prefs.highContrast} onChange={(_, checked) => setPref('highContrast', checked)} />}
            label={t('a11y.highContrast')}
          />
          <FormControlLabel
            control={<Switch checked={prefs.reduceMotion} onChange={(_, checked) => setPref('reduceMotion', checked)} />}
            label={t('a11y.reduceMotion')}
          />
          <FormControlLabel
            control={<Switch checked={prefs.underlineLinks} onChange={(_, checked) => setPref('underlineLinks', checked)} />}
            label={t('a11y.underlineLinks')}
          />
        </Stack>

        <Box sx={{ mt: 1.5 }}>
          <Button fullWidth variant="outlined" onClick={reset}>
            {t('a11y.reset')}
          </Button>
        </Box>
      </Popover>
    </>
  )
}
