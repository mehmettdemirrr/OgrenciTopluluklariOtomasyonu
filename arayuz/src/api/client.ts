import axios, { type InternalAxiosRequestConfig } from 'axios'
import { getSession, setSession } from '../auth/tokenStore'

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

async function performRefresh(): Promise<string | null> {
  const { csrfToken } = getSession()
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
