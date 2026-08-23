import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır — not opsiyonel serbest metin.
export const clubApplicationDecisionFormSchema = z.object({
  reviewNote: z.string(),
})

export type ClubApplicationDecisionFormValues = z.infer<typeof clubApplicationDecisionFormSchema>

export const emptyClubApplicationDecisionFormValues: ClubApplicationDecisionFormValues = { reviewNote: '' }
