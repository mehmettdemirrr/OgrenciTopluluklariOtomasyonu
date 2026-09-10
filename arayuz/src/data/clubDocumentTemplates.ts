/** Kuruluş formu şablonları — `arayuz/public/club-document-templates`. Seed kodu FR-0230, resmi form FR-0239. */
export const CLUB_DOCUMENT_TEMPLATES_ZIP = '/club-document-templates/tum-sablonlar.zip'

const files: Record<string, { href: string; fileName: string }> = {
  'FR-0239': {
    href: '/club-document-templates/FR-0239.docx',
    fileName: 'FR-0239-Topluluk-Akademik-Danisman.docx',
  },
  'FR-0240': {
    href: '/club-document-templates/FR-0240.docx',
    fileName: 'FR-0240-Topluluk-Asil-Uyeler.docx',
  },
  'FR-0241': {
    href: '/club-document-templates/FR-0241.docx',
    fileName: 'FR-0241-Topluluk-Faaliyet-Plani.docx',
  },
  'FR-0242': {
    href: '/club-document-templates/FR-0242.docx',
    fileName: 'FR-0242-Topluluk-Kapak.docx',
  },
  'FR-0243': {
    href: '/club-document-templates/FR-0243.docx',
    fileName: 'FR-0243-Topluluk-Kurucu-Uye-Dilekcesi.docx',
  },
  'FR-0244': {
    href: '/club-document-templates/FR-0244.docx',
    fileName: 'FR-0244-Topluluk-Kurulus-Dilekcesi.docx',
  },
  'FR-0245': {
    href: '/club-document-templates/FR-0245.docx',
    fileName: 'FR-0245-Topluluk-Uye-Listesi.docx',
  },
  'FR-0272': {
    href: '/club-document-templates/FR-0272.docx',
    fileName: 'FR-0272-Ogrenci-Topluluklari-Ornek-Tuzugu.docx',
  },
}

files['FR-0230'] = files['FR-0239']

export function clubDocumentTemplate(code: string) {
  return files[code] ?? null
}

export function resolveClubDocumentTemplate(type: {
  id: number
  code: string
  templateFileId?: number | null
}): { source: 'api' | 'static'; url: string; fileName: string } | null {
  if (type.templateFileId) {
    return {
      source: 'api',
      url: `/club-document-types/${type.id}/template`,
      fileName: `${type.code}-sablon`,
    }
  }

  const fallback = clubDocumentTemplate(type.code)
  if (!fallback) {
    return null
  }

  return { source: 'static', url: fallback.href, fileName: fallback.fileName }
}
