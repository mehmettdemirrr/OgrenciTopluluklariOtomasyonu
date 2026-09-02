import { useEffect } from 'react'
import { useLocale } from '../i18n/LocaleContext'

/**
 * docs/MIMARI.md · A-52: her sayfa kendi sekme başlığını yazar.
 * Başlık `null` verilirse (veri henüz yüklenmediyse) yalnızca kurum adı kalır — sekme
 * "undefined" yazmaz. Bileşen sökülünce başlık kuruma geri döner.
 */
export function useDocumentTitle(title: string | null | undefined) {
  const { t } = useLocale()
  const suffix = `${t('brand.university')} ${t('brand.name')}`

  useEffect(() => {
    document.title = title ? `${title} · ${suffix}` : suffix

    return () => {
      document.title = suffix
    }
  }, [title, suffix])
}
