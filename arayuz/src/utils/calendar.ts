/** docs/MIMARI.md · K-46: "takvime ekle" tamamen istemci tarafıdır; sunucuya .ics ucu eklenmez. */
function toIcsDate(iso: string): string {
  return new Date(iso).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '')
}

function escapeIcsText(value: string): string {
  return value.replace(/\\/g, '\\\\').replace(/\n/g, '\\n').replace(/,/g, '\\,').replace(/;/g, '\\;')
}

export function buildEventIcs(input: { id: number; title: string; startIso: string; endIso: string; location?: string | null; description?: string | null }): string {
  return [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//Ogrenci Topluluklari//TR',
    'BEGIN:VEVENT',
    `UID:event-${input.id}@ogrencitopluluklari`,
    `DTSTAMP:${toIcsDate(new Date().toISOString())}`,
    `DTSTART:${toIcsDate(input.startIso)}`,
    `DTEND:${toIcsDate(input.endIso)}`,
    `SUMMARY:${escapeIcsText(input.title)}`,
    input.location ? `LOCATION:${escapeIcsText(input.location)}` : '',
    input.description ? `DESCRIPTION:${escapeIcsText(input.description)}` : '',
    'END:VEVENT',
    'END:VCALENDAR',
  ].filter(Boolean).join('\r\n')
}

export function downloadEventIcs(input: Parameters<typeof buildEventIcs>[0]): void {
  const blob = new Blob([buildEventIcs(input)], { type: 'text/calendar;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `etkinlik-${input.id}.ics`
  link.click()
  URL.revokeObjectURL(url)
}
