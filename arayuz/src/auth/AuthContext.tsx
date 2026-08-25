import { createContext, useContext, useEffect, useState, useSyncExternalStore, type ReactNode } from 'react'
import { apiClient, restoreSession, type AuthResponse } from '../api/client'
import { getSession, setSession, setSessionEmail, subscribe } from './tokenStore'

interface AuthContextValue {
  isAuthenticated: boolean
  /**
   * A-59: açılıştaki sessiz refresh sürerken true. `ProtectedRoute` bu sırada **karar vermez** —
   * aksi hâlde token gelmeden verilen "anonim" kararı geri alınamaz ve kullanıcı her F5'te
   * giriş ekranına düşer.
   */
  isBootstrapping: boolean
  permissions: string[]
  email: string | null
  hasPermission: (permission: string) => boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const session = useSyncExternalStore(subscribe, getSession)
  const [isBootstrapping, setIsBootstrapping] = useState(true)

  useEffect(() => {
    let cancelled = false

    // Sonuç ne olursa olsun önyükleme biter: başarılıysa oturum geri gelmiştir,
    // başarısızsa ProtectedRoute artık /login'e yönlendirebilir.
    restoreSession().finally(() => {
      if (!cancelled) {
        setIsBootstrapping(false)
      }
    })

    return () => {
      cancelled = true
    }
  }, [])

  const login = async (email: string, password: string) => {
    const response = await apiClient.post<AuthResponse>('/auth/login', { email, password })
    setSession(response.data.accessToken, response.data.csrfToken)
    setSessionEmail(email)
  }

  const logout = async () => {
    try {
      await apiClient.post('/auth/logout')
    } finally {
      setSession(null, null)
    }
  }

  const value: AuthContextValue = {
    isAuthenticated: session.accessToken !== null,
    isBootstrapping,
    permissions: session.permissions,
    email: session.email,
    hasPermission: (permission) => session.permissions.includes(permission),
    login,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth yalnizca AuthProvider icinde kullanilabilir.')
  }
  return context
}
