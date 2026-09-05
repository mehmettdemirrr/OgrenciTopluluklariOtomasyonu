import { z } from 'zod'
import type { SocialPlatform } from '../api/types'

// Y-35: yalnızca biçim doğrulanır. https/alan adı kontrolü sunucudadır (Y-80).
export const clubContactFormSchema = z.object({
  contactEmail: z.string(),
  contactPhone: z.string(),
  links: z.array(
    z.object({
      platform: z.custom<SocialPlatform>(),
      url: z.string().min(1, 'Bağlantı adresi gerekli.'),
    }),
  ),
})

export type ClubContactFormValues = z.infer<typeof clubContactFormSchema>

export const emptyClubContactFormValues: ClubContactFormValues = {
  contactEmail: '',
  contactPhone: '',
  links: [],
}

export function toClubContactPayload(values: ClubContactFormValues) {
  return {
    contactEmail: values.contactEmail.trim() || null,
    contactPhone: values.contactPhone.trim() || null,
    links: values.links.map((link, index) => ({ platform: link.platform, url: link.url.trim(), displayOrder: index })),
  }
}
