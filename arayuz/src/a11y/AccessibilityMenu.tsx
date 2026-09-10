import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import ContrastOutlinedIcon from '@mui/icons-material/ContrastOutlined'
import CropLandscapeOutlinedIcon from '@mui/icons-material/CropLandscapeOutlined'
import DragHandleOutlinedIcon from '@mui/icons-material/DragHandleOutlined'
import FormatAlignLeftOutlinedIcon from '@mui/icons-material/FormatAlignLeftOutlined'
import FormatSizeOutlinedIcon from '@mui/icons-material/FormatSizeOutlined'
import HeightOutlinedIcon from '@mui/icons-material/HeightOutlined'
import InvertColorsOutlinedIcon from '@mui/icons-material/InvertColorsOutlined'
import LinkOutlinedIcon from '@mui/icons-material/LinkOutlined'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import MicNoneOutlinedIcon from '@mui/icons-material/MicNoneOutlined'
import MouseOutlinedIcon from '@mui/icons-material/MouseOutlined'
import ReplayOutlinedIcon from '@mui/icons-material/ReplayOutlined'
import SpaceBarOutlinedIcon from '@mui/icons-material/SpaceBarOutlined'
import VolumeUpOutlinedIcon from '@mui/icons-material/VolumeUpOutlined'
import ZoomInOutlinedIcon from '@mui/icons-material/ZoomInOutlined'
import {
  Box,
  Button,
  FormControlLabel,
  IconButton,
  Radio,
  RadioGroup,
  Stack,
  Switch,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { useEffect, useState } from 'react'
import { useLocale } from '../i18n/LocaleContext'
import {
  ALIGN_CYCLE,
  CONTRAST_CYCLE,
  FONT_SCALES,
  LEVEL_CYCLE,
  SATURATION_CYCLE,
  nextCycle,
  useAccessibility,
} from './AccessibilityContext'
import { readMainContent, speak, stopSpeaking } from './speech'

interface AccessibilityMenuProps {
  onClose: () => void
}

export function AccessibilityMenu({ onClose }: AccessibilityMenuProps) {
  const { t, locale } = useLocale()
  const { prefs, setPref, reset } = useAccessibility()
  const [speaking, setSpeaking] = useState(false)
  const [speechMessage, setSpeechMessage] = useState<string | null>(null)
  const lang = locale === 'tr' ? 'tr-TR' : 'en-US'
  const large = prefs.largeTools
  const side = prefs.panelSide

  useEffect(() => () => stopSpeaking(), [])

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const toggleScreenReader = () => {
    if (speaking) {
      stopSpeaking()
      setSpeaking(false)
      setSpeechMessage(null)
      return
    }
    const text = readMainContent()
    if (!text) {
      setSpeechMessage(t('a11y.nothingToRead'))
      return
    }
    const started = speak(text, lang, () => setSpeaking(false))
    setSpeaking(started)
    setSpeechMessage(started ? null : t('a11y.speechUnsupported'))
  }

  const handleReset = () => {
    if (!window.confirm(t('a11y.resetConfirm'))) {
      return
    }
    stopSpeaking()
    setSpeaking(false)
    setSpeechMessage(null)
    reset()
  }

  const contrastLabel =
    prefs.contrast === 'high' ? t('a11y.contrastHigh') : prefs.contrast === 'invert' ? t('a11y.contrastInvert') : t('a11y.contrast')
  const saturationLabel =
    prefs.saturation === 'low'
      ? t('a11y.saturationLow')
      : prefs.saturation === 'high'
        ? t('a11y.saturationHigh')
        : t('a11y.saturation')
  const zoomLabel = prefs.zoom > 0 ? t('a11y.zoomLevel', { level: prefs.zoom }) : t('a11y.zoom')
  const lineHeightLabel = prefs.lineHeight > 0 ? t('a11y.lineHeightLevel', { level: prefs.lineHeight }) : t('a11y.lineHeight')
  const spacingLabel =
    prefs.letterSpacing > 0 ? t('a11y.letterSpacingLevel', { level: prefs.letterSpacing }) : t('a11y.letterSpacing')

  return (
    <Box
      id="a11y-widget"
      role="dialog"
      aria-label={t('a11y.menuTitle')}
      sx={{
        position: 'fixed',
        top: 0,
        [side]: 0,
        zIndex: (theme) => theme.zIndex.modal,
        width: { xs: '100%', sm: large ? 450 : 380 },
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        bgcolor: 'background.default',
        boxShadow: (theme) => theme.shadows[16],
      }}
    >
      <Stack
        direction="row"
        sx={{
          alignItems: 'center',
          justifyContent: 'space-between',
          px: 2.5,
          py: 2,
          bgcolor: 'primary.main',
          color: 'common.white',
          flexShrink: 0,
        }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 700, fontSize: 18 }}>
          {t('a11y.menuTitle')}
        </Typography>
        <IconButton aria-label={t('a11y.close')} onClick={onClose} size="small" sx={{ color: 'common.white' }}>
          <CloseRoundedIcon />
        </IconButton>
      </Stack>

      <Box sx={{ p: 2.5, overflow: 'auto', flex: 1 }}>
        <FormControlLabel
          sx={{ mb: 2, ml: 0, mr: 0, width: '100%', justifyContent: 'space-between' }}
          labelPlacement="start"
          control={<Switch checked={prefs.largeTools} onChange={(_, checked) => setPref('largeTools', checked)} />}
          label={<Typography sx={{ fontWeight: 600 }}>{t('a11y.largeTools')}</Typography>}
        />

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
            gap: 1.5,
            alignItems: 'stretch',
          }}
        >
          <ToolCard
            large={large}
            selected={speaking}
            icon={<VolumeUpOutlinedIcon />}
            label={t('a11y.screenReader')}
            onClick={toggleScreenReader}
          />
          <ToolCard
            large={large}
            selected={prefs.selectionReader}
            icon={<MicNoneOutlinedIcon />}
            label={t('a11y.selectionReader')}
            onClick={() => setPref('selectionReader', !prefs.selectionReader)}
          />
          <ToolCard
            large={large}
            selected={prefs.underlineLinks}
            icon={<LinkOutlinedIcon />}
            label={t('a11y.highlightLinks')}
            onClick={() => setPref('underlineLinks', !prefs.underlineLinks)}
          />
          <ToolCard
            large={large}
            selected={prefs.fontScale !== 1}
            icon={<FormatSizeOutlinedIcon />}
            label={t('a11y.fontSize')}
            onClick={() => setPref('fontScale', nextCycle(FONT_SCALES, prefs.fontScale))}
          />
          <ToolCard
            large={large}
            selected={prefs.contrast !== 'off'}
            icon={<ContrastOutlinedIcon />}
            label={contrastLabel}
            onClick={() => setPref('contrast', nextCycle(CONTRAST_CYCLE, prefs.contrast))}
          />
          <ToolCard
            large={large}
            selected={prefs.zoom > 0}
            icon={<ZoomInOutlinedIcon />}
            label={zoomLabel}
            onClick={() => setPref('zoom', nextCycle(LEVEL_CYCLE, prefs.zoom))}
          />
          <ToolCard
            large={large}
            selected={prefs.textAlign !== 'start'}
            icon={<FormatAlignLeftOutlinedIcon />}
            label={t('a11y.textAlign')}
            onClick={() => setPref('textAlign', nextCycle(ALIGN_CYCLE, prefs.textAlign))}
          />
          <ToolCard
            large={large}
            selected={prefs.lineHeight > 0}
            icon={<HeightOutlinedIcon />}
            label={lineHeightLabel}
            onClick={() => setPref('lineHeight', nextCycle(LEVEL_CYCLE, prefs.lineHeight))}
          />
          <ToolCard
            large={large}
            selected={prefs.letterSpacing > 0}
            icon={<SpaceBarOutlinedIcon />}
            label={spacingLabel}
            onClick={() => setPref('letterSpacing', nextCycle(LEVEL_CYCLE, prefs.letterSpacing))}
          />
          <ToolCard
            large={large}
            selected={prefs.largeCursor}
            icon={<MouseOutlinedIcon />}
            label={t('a11y.largeCursor')}
            onClick={() => setPref('largeCursor', !prefs.largeCursor)}
          />
          <ToolCard
            large={large}
            selected={prefs.saturation !== 'off'}
            icon={<InvertColorsOutlinedIcon />}
            label={saturationLabel}
            onClick={() => setPref('saturation', nextCycle(SATURATION_CYCLE, prefs.saturation))}
          />
          <ToolCard
            large={large}
            selected={prefs.readingGuide}
            icon={<DragHandleOutlinedIcon />}
            label={t('a11y.readingGuide')}
            onClick={() => setPref('readingGuide', !prefs.readingGuide)}
          />
          <ToolCard
            large={large}
            selected={prefs.readingMask}
            icon={<CropLandscapeOutlinedIcon />}
            label={t('a11y.readingMask')}
            onClick={() => setPref('readingMask', !prefs.readingMask)}
          />
          <ToolCard
            large={large}
            selected={prefs.dyslexiaFriendly}
            icon={<MenuBookOutlinedIcon />}
            label={t('a11y.dyslexia')}
            onClick={() => setPref('dyslexiaFriendly', !prefs.dyslexiaFriendly)}
          />
        </Box>

        {speechMessage && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.5 }}>
            {speechMessage}
          </Typography>
        )}

        <FormControlLabel
          sx={{ mt: 2, ml: 0 }}
          control={<Switch checked={prefs.reduceMotion} onChange={(_, checked) => setPref('reduceMotion', checked)} />}
          label={t('a11y.reduceMotion')}
        />

        <Button
          fullWidth
          variant="contained"
          onClick={handleReset}
          startIcon={<ReplayOutlinedIcon />}
          sx={{
            mt: 2.5,
            py: 1.5,
            fontWeight: 700,
            bgcolor: 'text.secondary',
            color: 'common.white',
            '&:hover': { bgcolor: 'text.primary' },
          }}
        >
          {t('a11y.resetAll')}
        </Button>

        <RadioGroup
          row
          value={prefs.panelSide}
          onChange={(_, value) => setPref('panelSide', value === 'left' ? 'left' : 'right')}
          sx={{ mt: 2.5, pt: 2.5, borderTop: 1, borderColor: 'divider', gap: 2 }}
        >
          <FormControlLabel value="left" control={<Radio size="small" />} label={t('a11y.showLeft')} />
          <FormControlLabel value="right" control={<Radio size="small" />} label={t('a11y.showRight')} />
        </RadioGroup>
      </Box>
    </Box>
  )
}

function ToolCard({
  icon,
  label,
  selected,
  large,
  onClick,
}: {
  icon: ReactNode
  label: string
  selected: boolean
  large: boolean
  onClick: () => void
}) {
  return (
    <Button
      type="button"
      variant="outlined"
      onClick={onClick}
      aria-pressed={selected}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 1,
        minHeight: large ? 112 : 90,
        px: 1.25,
        py: large ? 2.5 : 1.9,
        borderRadius: 2,
        borderWidth: 2,
        color: selected ? 'common.white' : 'text.primary',
        borderColor: selected ? 'primary.main' : 'divider',
        bgcolor: selected ? 'primary.main' : 'background.paper',
        boxShadow: selected ? (theme) => theme.shadows[2] : 'none',
        '&:hover': {
          borderColor: 'primary.main',
          bgcolor: selected ? 'primary.dark' : 'action.hover',
          color: selected ? 'common.white' : 'text.primary',
        },
        '& .MuiSvgIcon-root': {
          fontSize: large ? 36 : 24,
          color: 'inherit',
        },
      }}
    >
      {icon}
      <Typography sx={{ fontSize: large ? 14 : 12, fontWeight: 600, lineHeight: 1.2, textAlign: 'center' }}>{label}</Typography>
    </Button>
  )
}
