import { createContext, useContext, useSyncExternalStore, type ReactNode } from 'react'
import { apiClient, type AuthResponse } from '../api/client'
import { getSession, setSession, subscribe } from './tokenStore'

interface AuthContextValue {
  isAuthenticated: boolean
  permissions: string[]
  hasPermission: (permission: string) => boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const session = useSyncExternalStore(subscribe, getSession)

  const login = async (email: string, password: string) => {
    const response = await apiClient.post<AuthResponse>('/auth/login', { email, password })
    setSession(response.data.accessToken, response.data.csrfToken)
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
    permissions: session.permissions,
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
