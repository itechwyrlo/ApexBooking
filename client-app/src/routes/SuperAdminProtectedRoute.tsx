import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { AppBootScreen } from '../components/common/AppBootScreen'
import { useAuth } from '../hooks/useAuth'

interface ISuperAdminProtectedRouteProps {
  children: ReactNode
}

export function SuperAdminProtectedRoute({ children }: ISuperAdminProtectedRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <AppBootScreen label="Checking your session..." />
  }

  if (!isAuthenticated || !user?.isPlatformAdmin) {
    return <Navigate to="/superadmin/login" replace />
  }

  return <>{children}</>
}
