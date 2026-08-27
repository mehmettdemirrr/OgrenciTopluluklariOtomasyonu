import { z } from 'zod'

// Y-35: yalnızca biçim. "Bu kod alınmış mı" ve "zorunlu evraklar tam mı" kararları API'nin.
export const clubDocumentTypeFormSchema = z.object({
  code: z.string().min(1, 'Kod gerekli.').max(50),
  name: z.string().min(1, 'Ad gerekli.').max(300),
  isRequired: z.boolean(),
  isActive: z.boolean(),
  displayOrder: z.number().int().min(0),
})

export type ClubDocumentTypeFormValues = z.infer<typeof clubDocumentTypeFormSchema>

export const emptyClubDocumentTypeFormValues: ClubDocumentTypeFormValues = {
  code: '',
  name: '',
  isRequired: true,
  isActive: true,
  displayOrder: 0,
}
