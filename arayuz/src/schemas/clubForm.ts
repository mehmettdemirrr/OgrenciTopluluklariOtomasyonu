import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır. "Bu isimde topluluk var mı" API'nin kararı.
export const clubFormSchema = z.object({
  name: z.string().min(1, 'Topluluk adı gerekli.'),
  description: z.string(),
})

export type ClubFormValues = z.infer<typeof clubFormSchema>

export const emptyClubFormValues: ClubFormValues = { name: '', description: '' }

export const createClubFormSchema = clubFormSchema.extend({
  advisorId: z.number({ error: 'Danışman seçin.' }).int().positive('Danışman seçin.'),
})

export type CreateClubFormValues = z.infer<typeof createClubFormSchema>

export const emptyCreateClubFormValues: CreateClubFormValues = { name: '', description: '', advisorId: 0 }
