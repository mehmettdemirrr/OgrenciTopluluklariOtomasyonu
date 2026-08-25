import axios, { type InternalAxiosRequestConfig } from 'axios'
import { getSession, hasSessionHint, setSession } from '../auth/tokenStore'

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAtUtc: string
  csrfToken: string
}

interface RetriableRequestConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

export const apiClient = axios.create({ baseURL: '/api', withCredentials: true })

apiClient.interceptors.request.use((config) => {
  const { accessToken } = getSession()
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})

let refreshInFlight: Promise<string | null> | null = null

/**
 * A-59: sayfa yenilendiğinde bellekteki CSRF istek token'ı da kaybolur. Sunucudan yeni bir çift
 * istenir — `__Host-Csrf` çerezi ve ona karşılık gelen istek token'ı birlikte üretilir (Y-48).
 */
async function fetchCsrfToken(): Promise<string | null> {
  try {
    const response = await axios.get<{ csrfToken: string }>('/api/auth/csrf', { withCredentials: true })
    return response.data.csrfToken
  } catch {
    return null
  }
}

async function performRefresh(): Promise<string | null> {
  const csrfToken = getSession().csrfToken ?? (await fetchCsrfToken())
  if (!csrfToken) {
    setSession(null, null)
    return null
  }

  try {
    const response = await axios.post<AuthResponse>('/api/auth/refresh', null, {
      withCredentials: true,
      headers: { 'X-XSRF-TOKEN': csrfToken },
    })
    setSession(response.data.accessToken, response.data.csrfToken)
    return response.data.accessToken
  } catch {
    setSession(null, null)
    return null
  }
}

/**
 * A-59: uygulama açılışında **bir kez** çalışan sessiz oturum kurtarma.
 *
 * Bunsuz F5 sonrası şu oluyordu: bellekteki token gider → `ProtectedRoute` anında `/login`'e
 * yönlendirir → refresh çerezi hâlâ geçerli olmasına rağmen hiç kullanılmaz. Interceptor ancak
 * bir 401 alınca devreye giriyor, oysa hiç istek atılmıyor.
 */
export function restoreSession(): Promise<string | null> {
  // Hiç oturum açılmamış bir ziyaretçide denemenin karşılığı yok — anonim vitrin sayfaları
  // her açılışta iki gereksiz istek göndermesin (Faz 14'ün /api/public/* muafiyetiyle aynı fikir).
  if (!hasSessionHint()) {
    return Promise.resolve(null)
  }

  refreshInFlight ??= performRefresh().finally(() => {
    refreshInFlight = null
  })

  return refreshInFlight
}

// K-01: 401 alan istekler, sessiz refresh'ten sonra TEK sefer yeniden denenir — refresh'in
// kendisi 401 dönerse (örn. çerez de süresi dolmuş) sonsuz döngüye girmemek için _retried
// bayrağı ve /auth/refresh'in kendisini asla yeniden denememek gerekir.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined
    const isRefreshCall = originalRequest?.url?.includes('/auth/refresh')
    // Faz 14 · A-42: /api/public/* anonim ziyaretçiden de çağrılır — çerez/CSRF yok, sessiz refresh
    // denemesi burada anlamsız ve gereksiz bir /auth/refresh isteğine yol açar.
    const isPublicCall = originalRequest?.url?.includes('/public/')

    if (error.response?.status === 401 && originalRequest && !originalRequest._retried && !isRefreshCall && !isPublicCall) {
      originalRequest._retried = true

      if (!refreshInFlight) {
        refreshInFlight = performRefresh().finally(() => {
          refreshInFlight = null
        })
      }

      const newToken = await refreshInFlight
      if (newToken) {
        originalRequest.headers.set('Authorization', `Bearer ${newToken}`)
        return apiClient(originalRequest)
      }
    }

    return Promise.reject(error)
  },
)
