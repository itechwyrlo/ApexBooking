import type { ReactNode } from 'react'
import { Navigate, useParams } from 'react-router-dom'
import { AppBootScreen } from '../components/common/AppBootScreen'
import { useAuth } from '../hooks/useAuth'
import { getDashboardPath } from '../config/dashboardRoutes'
import type { Role } from '../types/Role'

interface IProtectedRouteProps {
  children: ReactNode
  allowedRoles?: Role[]
}

export function ProtectedRoute({ children, allowedRoles }: IProtectedRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth()
  // Every route this guards is nested under /:slug/dashboard, so the slug the user was already
  // on is available here — redirect back to that same business's login, not a path-less one.
  const { slug } = useParams<{ slug: string }>()

  if (isLoading) {
    return <AppBootScreen label="Checking your session..." />
  }

  if (!isAuthenticated) {
    return <Navigate to={slug ? `/${slug}/login` : '/'} replace />
  }

  if (allowedRoles && user && !allowedRoles.some((role) => user.roles.includes(role))) {
    return <Navigate to={getDashboardPath(user)} replace />
  }

  return <>{children}</>
}
