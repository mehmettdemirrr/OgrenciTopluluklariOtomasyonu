import { jwtDecode } from 'jwt-decode'

interface AccessTokenClaims {
  permission?: string | string[]
}

interface AuthSession {
  accessToken: string | null
  csrfToken: string | null
  permissions: string[]
}

const emptySession: AuthSession = { accessToken: null, csrfToken: null, permissions: [] }

let session: AuthSession = emptySession
const listeners = new Set<() => void>()

function decodePermissions(accessToken: string): string[] {
  try {
    const claims = jwtDecode<AccessTokenClaims>(accessToken)
    if (!claims.permission) return []
    return Array.isArray(claims.permission) ? claims.permission : [claims.permission]
  } catch {
    return []
  }
}

// K-14/Y-39: access token yalnızca bellekte (bu modülde) tutulur, hiçbir zaman localStorage/
// sessionStorage'a yazılmaz — refresh çerezi zaten httpOnly, buradaki token da sayfa
// yenilendiğinde kaybolur ve sessiz refresh akışı onu yeniden üretir.
export function setSession(accessToken: string | null, csrfToken: string | null): void {
  session = accessToken
    ? { accessToken, csrfToken, permissions: decodePermissions(accessToken) }
    : emptySession
  listeners.forEach((listener) => listener())
}

export function getSession(): AuthSession {
  return session
}

export function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
