export function formatAnnouncementPill(iso: string, locale: string) {
  const date = new Date(iso)
  const day = date.toLocaleDateString(locale, { day: '2-digit' })
  const month = date.toLocaleDateString(locale, { month: 'short' }).replace('.', '')
  return `${day} ${month}`
}

export function formatAnnouncementDateTime(iso: string, locale: string) {
  return new Date(iso).toLocaleString(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function announcementExcerpt(content: string, max = 160) {
  const text = content.replace(/\s+/g, ' ').trim()
  if (text.length <= max) {
    return text
  }
  return `${text.slice(0, max).trimEnd()}…`
}
