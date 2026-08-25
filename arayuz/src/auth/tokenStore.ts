import { jwtDecode } from 'jwt-decode'

interface AccessTokenClaims {
  permission?: string | string[]
}

interface AuthSession {
  accessToken: string | null
  csrfToken: string | null
  permissions: string[]
  email: string | null
}

const emptySession: AuthSession = { accessToken: null, csrfToken: null, permissions: [], email: null }

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
//
// `email` JWT claim'i değildir (bugün token yalnızca NameIdentifier + permission taşıyor —
// backend'e dokunmadan eklenemez, Faz 8'in "sıfır backend değişikliği" şartı). Bunun yerine
// login() çağrısının zaten bildiği e-posta burada saklanır; sessiz refresh'te dokunulmaz.
// A-59: "bu tarayıcıda bir oturum açılmıştı" ipucu. Token DEĞİL, taşıdığı tek bilgi bir bayrak —
// K-01 ihlal edilmez. Amacı, anonim ziyaretçinin her sayfa açılışında boşuna /auth/csrf +
// /auth/refresh çifti göndermesini önlemek; refresh çerezi httpOnly olduğu için varlığı
// JavaScript'ten başka türlü anlaşılamaz.
const SESSION_HINT_KEY = 'auth.session-hint'

export function hasSessionHint(): boolean {
  try {
    return localStorage.getItem(SESSION_HINT_KEY) === '1'
  } catch {
    return false
  }
}

function writeSessionHint(active: boolean): void {
  try {
    if (active) {
      localStorage.setItem(SESSION_HINT_KEY, '1')
    } else {
      localStorage.removeItem(SESSION_HINT_KEY)
    }
  } catch {
    // Gizli sekme / depolama kapalı: ipucu olmadan da çalışır, yalnızca bir istek fazla gider.
  }
}

export function setSession(accessToken: string | null, csrfToken: string | null): void {
  session = accessToken
    ? { accessToken, csrfToken, permissions: decodePermissions(accessToken), email: session.email }
    : emptySession

  writeSessionHint(accessToken !== null)
  listeners.forEach((listener) => listener())
}

export function setSessionEmail(email: string): void {
  session = { ...session, email }
  listeners.forEach((listener) => listener())
}

export function getSession(): AuthSession {
  return session
}

export function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
