import { useEffect, useState } from 'react'

/**
 * docs/MIMARI.md · A-50: arama kutusuna her harf bir istek üretmesin diye 300 ms bekletir.
 * Y-62 aramayı sunucuya taşıdığı için gecikme artık bir gereklilik, kozmetik değil.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs)
    return () => window.clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
