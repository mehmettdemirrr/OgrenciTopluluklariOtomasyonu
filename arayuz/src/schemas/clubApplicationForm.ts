import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır — ad çakışması/danışman geçerliliği API'nin kararı.
export const clubApplicationFormSchema = z.object({
  proposedName: z.string().min(1, 'Topluluk adı gerekli.'),
  description: z.string(),
  justification: z.string().min(1, 'Gerekçe gerekli.'),
  proposedAdvisorId: z.number({ error: 'Danışman seçin.' }).int().positive('Danışman seçin.'),
  // K-35: 0 = kategori seçilmedi; API'ye null gider.
  proposedCategoryId: z.number().int(),
})

export type ClubApplicationFormValues = z.infer<typeof clubApplicationFormSchema>

export const emptyClubApplicationFormValues: ClubApplicationFormValues = {
  proposedName: '',
  description: '',
  justification: '',
  proposedAdvisorId: 0,
  proposedCategoryId: 0,
}
