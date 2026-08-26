import { z } from 'zod'
import type { AcademicTermListItemDto } from '../api/types'

// Y-35: yalnızca biçim doğrulanır — "pencere şu anda açık mı" kararı API'nin.
export const clubApplicationWindowFormSchema = z
  .object({
    override: z.enum(['FollowSchedule', 'ForceOpen', 'ForceClosed']),
    startDate: z.string(),
    endDate: z.string(),
  })
  .refine(
    (values) =>
      values.startDate.trim() === '' ||
      values.endDate.trim() === '' ||
      new Date(values.endDate) > new Date(values.startDate),
    { message: 'Bitiş tarihi başlangıçtan sonra olmalı.', path: ['endDate'] },
  )
  .refine((values) => values.override !== 'FollowSchedule' || (values.startDate.trim() !== '' && values.endDate.trim() !== ''), {
    message: 'Takvime uymak için iki tarih de gerekli. Aksi hâlde başvurular kapalı kalır.',
    path: ['startDate'],
  })

export type ClubApplicationWindowFormValues = z.infer<typeof clubApplicationWindowFormSchema>

export const emptyClubApplicationWindowFormValues: ClubApplicationWindowFormValues = {
  override: 'FollowSchedule',
  startDate: '',
  endDate: '',
}

export function toWindowFormValues(term: AcademicTermListItemDto): ClubApplicationWindowFormValues {
  return {
    override: term.clubApplicationOverride,
    startDate: term.clubApplicationStartUtc?.slice(0, 10) ?? '',
    endDate: term.clubApplicationEndUtc?.slice(0, 10) ?? '',
  }
}

export function toWindowPayload(values: ClubApplicationWindowFormValues) {
  return {
    override: values.override,
    startUtc: values.startDate.trim() === '' ? null : new Date(`${values.startDate}T00:00:00Z`).toISOString(),
    // Bitiş günü dahil olsun diye günün sonuna çekilir — yönetici "10 Ekim'e kadar" derken
    // 10 Ekim'i kastediyor. Aralık kontrolü sunucuda kapsayıcı (A-66).
    endUtc: values.endDate.trim() === '' ? null : new Date(`${values.endDate}T23:59:59Z`).toISOString(),
  }
}
