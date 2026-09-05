import { z } from 'zod'

// docs/MIMARI.md · K-42/A-71: içerik RichTextEditor'den JSON ağacı olarak gelir; düz metin
// aynası (Content) sunucuda türetilir, arayüz ayrıca göndermez.
// Y-35: yalnızca biçim doğrulanır. Y-57: görünürlük varsayılana güvenilmeden arayüzde açıkça seçilir.
export const announcementFormSchema = z.object({
  title: z.string().min(1, 'Başlık gerekli.'),
  contentJson: z.string().min(1, 'İçerik gerekli.'),
  visibility: z.enum(['Members', 'Public']),
})

export type AnnouncementFormValues = z.infer<typeof announcementFormSchema>
