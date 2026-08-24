import { useEffect } from 'react'

const SUFFIX = 'Malatya Turgut Özal Üniversitesi Öğrenci Toplulukları'

/**
 * docs/MIMARI.md · A-52: her sayfa kendi sekme başlığını yazar.
 * Başlık `null` verilirse (veri henüz yüklenmediyse) yalnızca kurum adı kalır — sekme
 * "undefined" yazmaz. Bileşen sökülünce başlık kuruma geri döner.
 */
export function useDocumentTitle(title: string | null | undefined) {
  useEffect(() => {
    document.title = title ? `${title} · ${SUFFIX}` : SUFFIX

    return () => {
      document.title = SUFFIX
    }
  }, [title])
}
