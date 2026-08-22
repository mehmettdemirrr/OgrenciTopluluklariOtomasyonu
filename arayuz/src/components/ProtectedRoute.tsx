import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

interface ProtectedRouteProps {
  children: ReactNode
  requiredPermission?: string
}

// Y-45: bu yalnızca görüntü katmanı gizlemesidir — asıl yetki kararı her istekte API'nin
// [SecuredOperation] aspect'i tarafından yeniden verilir, burası sadece kullanılamayacak
// bir ekranı önceden gizleyip gereksiz 403 tıklamasını önler.
export function ProtectedRoute({ children, requiredPermission }: ProtectedRouteProps) {
  const { isAuthenticated, hasPermission } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <Navigate to="/panel" replace />
  }

  return <>{children}</>
}
