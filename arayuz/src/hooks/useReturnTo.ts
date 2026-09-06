import { useCallback } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'

/**
 * docs/MIMARI.md · A-78: formu hangi ekranın açtığı `returnTo` ile taşınır.
 * Açık yönlendirme (open redirect) olmasın diye YALNIZCA uygulama içi, "/" ile başlayan
 * ve "//" ile başlamayan adresler kabul edilir; aksi hâlde fallback kullanılır.
 */
export function useReturnTo(fallback: string) {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const raw = searchParams.get('returnTo')
  const returnTo = raw && raw.startsWith('/') && !raw.startsWith('//') ? raw : fallback

  const goBack = useCallback(() => navigate(returnTo, { replace: true }), [navigate, returnTo])

  return { returnTo, goBack }
}
