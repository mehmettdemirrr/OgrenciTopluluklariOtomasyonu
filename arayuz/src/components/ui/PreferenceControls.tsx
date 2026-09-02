import { IconButton, Stack, ToggleButton, ToggleButtonGroup, Tooltip } from '@mui/material'
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined'
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined'
import { useLocale } from '../../i18n/LocaleContext'
import { useThemeMode } from '../../theme/ThemeModeContext'

export function PreferenceControls() {
  const { mode, toggleMode } = useThemeMode()
  const { locale, setLocale, t } = useLocale()
  const themeLabel = mode === 'dark' ? t('prefs.themeLight') : t('prefs.themeDark')

  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Tooltip title={themeLabel}>
        <IconButton onClick={toggleMode} aria-label={themeLabel} size="small" color="inherit">
          {mode === 'dark' ? <LightModeOutlinedIcon fontSize="small" /> : <DarkModeOutlinedIcon fontSize="small" />}
        </IconButton>
      </Tooltip>
      <ToggleButtonGroup
        exclusive
        size="small"
        value={locale}
        onChange={(_, value: 'tr' | 'en' | null) => {
          if (value) setLocale(value)
        }}
        aria-label={t('prefs.language')}
        sx={{
          '& .MuiToggleButton-root': {
            px: 1,
            py: 0.25,
            minWidth: 36,
            fontSize: 12,
          },
        }}
      >
        <ToggleButton value="tr">TR</ToggleButton>
        <ToggleButton value="en">EN</ToggleButton>
      </ToggleButtonGroup>
    </Stack>
  )
}
