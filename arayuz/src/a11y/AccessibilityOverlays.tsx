import { alpha, Box } from '@mui/material'
import { useEffect, useState } from 'react'
import { useLocale } from '../i18n/LocaleContext'
import { useAccessibility } from './AccessibilityContext'
import { readSelection, speak, stopSpeaking } from './speech'

/** Fakülte widget’ındaki okuma kılavuzu, maske ve seçili alan okuyucu. */
export function AccessibilityOverlays() {
  const { prefs } = useAccessibility()
  const { locale } = useLocale()
  const lang = locale === 'tr' ? 'tr-TR' : 'en-US'
  const [pointer, setPointer] = useState({ x: 50, y: 50 })

  useEffect(() => {
    if (!prefs.readingGuide && !prefs.readingMask) {
      return
    }
    const onMove = (event: MouseEvent) => {
      setPointer({
        x: (event.clientX / window.innerWidth) * 100,
        y: (event.clientY / window.innerHeight) * 100,
      })
    }
    window.addEventListener('mousemove', onMove, { passive: true })
    return () => window.removeEventListener('mousemove', onMove)
  }, [prefs.readingGuide, prefs.readingMask])

  useEffect(() => {
    if (!prefs.selectionReader) {
      return
    }
    const onSelect = () => {
      const text = readSelection()
      if (text) {
        speak(text, lang)
      }
    }
    document.addEventListener('mouseup', onSelect)
    document.addEventListener('keyup', onSelect)
    return () => {
      document.removeEventListener('mouseup', onSelect)
      document.removeEventListener('keyup', onSelect)
      stopSpeaking()
    }
  }, [prefs.selectionReader, lang])

  const guideX = Math.min(90, Math.max(10, pointer.x))
  const guideY = Math.min(95, Math.max(5, pointer.y))
  const maskHeight = 15
  const maskTop = Math.min(100 - maskHeight, Math.max(0, pointer.y - maskHeight / 2))
  const maskBottom = maskTop + maskHeight

  return (
    <>
      {prefs.readingGuide && (
        <>
          <Box
            aria-hidden
            sx={{
              position: 'fixed',
              left: `${guideX}%`,
              top: `${guideY}%`,
              width: '70vw',
              maxWidth: 1000,
              height: 8,
              bgcolor: 'common.black',
              borderTop: 2,
              borderBottom: 2,
              borderColor: 'accent.main',
              zIndex: (theme) => theme.zIndex.modal - 2,
              pointerEvents: 'none',
              transform: 'translate(-50%, -50%)',
            }}
          />
          <Box
            aria-hidden
            sx={{
              position: 'fixed',
              left: `${guideX}%`,
              top: `${guideY}%`,
              width: '80vw',
              maxWidth: 1200,
              height: 60,
              zIndex: (theme) => theme.zIndex.modal - 3,
              pointerEvents: 'none',
              transform: 'translate(-50%, -50%)',
            }}
          />
        </>
      )}
      {prefs.readingMask && (
        <>
          <Box
            aria-hidden
            sx={{
              position: 'fixed',
              inset: 0,
              bgcolor: 'common.black',
              opacity: 0.75,
              zIndex: (theme) => theme.zIndex.modal - 3,
              pointerEvents: 'none',
              clipPath: `polygon(0% 0%, 100% 0%, 100% ${maskTop}%, 0% ${maskTop}%, 0% ${maskBottom}%, 100% ${maskBottom}%, 100% 100%, 0% 100%)`,
            }}
          />
          <Box
            aria-hidden
            sx={{
              position: 'fixed',
              left: 0,
              top: `${maskTop}%`,
              width: '100%',
              height: `${maskHeight}%`,
              borderTop: 3,
              borderBottom: 3,
              borderColor: 'primary.main',
              boxShadow: (theme) =>
                `inset 0 0 30px ${alpha(theme.palette.primary.main, 0.3)}, 0 0 40px ${alpha(theme.palette.primary.main, 0.4)}`,
              zIndex: (theme) => theme.zIndex.modal - 2,
              pointerEvents: 'none',
            }}
          />
        </>
      )}
    </>
  )
}
