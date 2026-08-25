import { z } from 'zod'

const STUDENT_ROLE = 'Member'
const ADVISOR_ROLE = 'Advisor'

/**
 * docs/MIMARI.md · Y-35: arayüz yalnızca BİÇİM doğrular; asıl kural API'de (Y-67).
 * Buradaki koşullu zorunluluklar kullanıcıyı boşuna sunucuya göndermemek içindir —
 * eksik alanla gönderilse API 400 döner ve kullanıcı hiç oluşturulmaz.
 */
export const createUserFormSchema = z
  .object({
    firstName: z.string().max(100, 'En fazla 100 karakter.'),
    lastName: z.string().max(100, 'En fazla 100 karakter.'),
    email: z.string().min(1, 'E-posta gerekli.').email('Geçerli bir e-posta girin.'),
    password: z.string().min(8, 'Parola en az 8 karakter olmalı.'),
    roleNames: z.array(z.string()),
    studentNumber: z.string(),
    departmentId: z.number(),
    enrollmentYear: z.string(),
    title: z.string(),
  })
  .superRefine((values, ctx) => {
    const needsStudent = values.roleNames.includes(STUDENT_ROLE)
    const needsAdvisor = values.roleNames.includes(ADVISOR_ROLE)

    if ((needsStudent || needsAdvisor) && !values.departmentId) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['departmentId'], message: 'Seçilen rol için bölüm zorunlu.' })
    }

    if (needsStudent) {
      if (values.studentNumber.trim() === '') {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['studentNumber'], message: 'Öğrenci numarası zorunlu.' })
      }

      const year = Number(values.enrollmentYear)
      if (!Number.isInteger(year) || year < 1900 || year > 2200) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['enrollmentYear'], message: 'Geçerli bir kayıt yılı girin.' })
      }
    }

    if (needsAdvisor && values.title.trim() === '') {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['title'], message: 'Akademik unvan zorunlu.' })
    }
  })

export type CreateUserFormValues = z.infer<typeof createUserFormSchema>

export const emptyCreateUserFormValues: CreateUserFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  roleNames: [],
  studentNumber: '',
  departmentId: 0,
  enrollmentYear: String(new Date().getFullYear()),
  title: '',
}
