import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır — isim tekilliği (idempotent POST) API'nin kararı.
export const nameFormSchema = z.object({
  name: z.string().min(1, 'Ad gerekli.'),
})

export type NameFormValues = z.infer<typeof nameFormSchema>

export const emptyNameFormValues: NameFormValues = { name: '' }

export const academicTermFormSchema = z
  .object({
    name: z.string().min(1, 'Dönem adı gerekli.'),
    startDate: z.string().min(1, 'Başlangıç tarihi gerekli.'),
    endDate: z.string().min(1, 'Bitiş tarihi gerekli.'),
  })
  .refine((values) => new Date(values.endDate) > new Date(values.startDate), {
    message: 'Bitiş tarihi başlangıçtan sonra olmalı.',
    path: ['endDate'],
  })

export type AcademicTermFormValues = z.infer<typeof academicTermFormSchema>

export const emptyAcademicTermFormValues: AcademicTermFormValues = { name: '', startDate: '', endDate: '' }
