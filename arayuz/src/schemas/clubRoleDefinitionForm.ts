import { z } from 'zod'

// Y-35: yalnızca biçim. "Bu unvan alınmış mı", "ikinci başkan üretir mi" kararları API'nin.
export const clubRoleDefinitionFormSchema = z.object({
  name: z.string().min(1, 'Unvan adı gerekli.').max(100),
  clubRole: z.enum(['Member', 'Officer', 'President']),
  // A-68: bit maskesi. Tanımsız bit gönderilirse API 400 döner (Y-69) — sınır kontrolü orada.
  capabilities: z.number().int().min(0),
  displayOrder: z.number().int().min(0),
})

export type ClubRoleDefinitionFormValues = z.infer<typeof clubRoleDefinitionFormSchema>

export const emptyClubRoleDefinitionFormValues: ClubRoleDefinitionFormValues = {
  name: '',
  clubRole: 'Member',
  capabilities: 0,
  displayOrder: 0,
}
