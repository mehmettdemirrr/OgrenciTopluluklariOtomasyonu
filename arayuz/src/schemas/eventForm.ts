import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır — zorunlu alan, tarih sırası, pozitif sayı.
// "Etkinlik yayınlanabilir mi", kontenjan doldu mu, bu öğrenci üye mi gibi kararlar API'de kalır.
export const eventFormSchema = z
  .object({
    // A-78: yalnızca kulüp bağlamı olmadan açılan formda doldurulur; 0 = seçilmedi.
    clubId: z.number().int(),
    title: z.string().min(1, 'Başlık gerekli.'),
    // docs/MIMARI.md · K-43/A-71: açıklama RichTextEditor'den JSON ağacı olarak gelir; düz metin
    // aynası (description) sunucuda türetilir, arayüz ayrıca göndermez.
    descriptionJson: z.string(),
    location: z.string(),
    startDateTime: z.string().min(1, 'Başlangıç tarihi gerekli.'),
    endDateTime: z.string().min(1, 'Bitiş tarihi gerekli.'),
    capacity: z.string(),
    // K-38: kitle seçimi zorunlu; varsayılan "herkese açık".
    audience: z.enum(['Public', 'ClubMembers']),
  })
  .refine((values) => new Date(values.endDateTime) > new Date(values.startDateTime), {
    message: 'Bitiş tarihi başlangıçtan sonra olmalı.',
    path: ['endDateTime'],
  })
  .refine((values) => values.capacity.trim() === '' || Number(values.capacity) > 0, {
    message: 'Kontenjan pozitif bir sayı olmalı.',
    path: ['capacity'],
  })
  .refine((values) => values.clubId > 0, { message: 'Topluluk seçin.', path: ['clubId'] })

export type EventFormValues = z.infer<typeof eventFormSchema>

export const emptyEventFormValues: EventFormValues = {
  clubId: 0,
  title: '',
  descriptionJson: '',
  location: '',
  startDateTime: '',
  endDateTime: '',
  capacity: '',
  audience: 'Public',
}

/** `datetime-local` alanı yerel saat bekler; sunucudan gelen ISO/UTC değeri dönüştürülür. */
export function toLocalInputValue(iso: string): string {
  const date = new Date(iso)
  const offset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

export function toEventPayload(values: EventFormValues) {
  return {
    title: values.title,
    descriptionJson: values.descriptionJson.trim() || null,
    location: values.location.trim() || null,
    startDateUtc: new Date(values.startDateTime).toISOString(),
    endDateUtc: new Date(values.endDateTime).toISOString(),
    capacity: values.capacity.trim() === '' ? null : Number(values.capacity),
    audience: values.audience,
  }
}

// docs/MIMARI.md · A-71: eski düz metin etkinlik düzenlemeye açılınca kaybolmasın diye
// editöre tek paragraflık bir belge olarak yüklenir.
export function plainTextToDoc(text: string): string {
  return JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: text ? [{ type: 'text', text }] : [] }],
  })
}
