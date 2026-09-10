const MAX_CHARS = 4000

export function stopSpeaking() {
  if (typeof window === 'undefined' || !window.speechSynthesis) {
    return
  }
  window.speechSynthesis.cancel()
}

export function speak(text: string, lang: string, onEnd?: () => void): boolean {
  if (typeof window === 'undefined' || !window.speechSynthesis) {
    return false
  }
  const trimmed = text.replace(/\s+/g, ' ').trim()
  if (!trimmed) {
    return false
  }
  stopSpeaking()
  const utterance = new SpeechSynthesisUtterance(trimmed.slice(0, MAX_CHARS))
  utterance.lang = lang
  utterance.onend = () => onEnd?.()
  utterance.onerror = () => onEnd?.()
  window.speechSynthesis.speak(utterance)
  return true
}

export function readMainContent(): string {
  const main = document.getElementById('main-content')
  return (main?.innerText ?? document.body.innerText ?? '').replace(/\s+/g, ' ').trim()
}

export function readSelection(): string {
  return window.getSelection()?.toString().replace(/\s+/g, ' ').trim() ?? ''
}
