import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır. Y-57: görünürlük varsayılana güvenilmeden arayüzde açıkça seçilir.
export const announcementFormSchema = z.object({
  title: z.string().min(1, 'Başlık gerekli.'),
  content: z.string().min(1, 'İçerik gerekli.'),
  visibility: z.enum(['Members', 'Public']),
})

export type AnnouncementFormValues = z.infer<typeof announcementFormSchema>
