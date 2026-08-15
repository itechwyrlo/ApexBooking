import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { AppBootScreen } from '../components/common/AppBootScreen'
import { useAuth } from '../hooks/useAuth'
import { getDashboardPath } from '../config/dashboardRoutes'
import { getPwaAccountKind, getPwaLastTenantSlug } from '../utils/pwaSessionHints'

// The installed PWA's start_url (see vite.config.ts). A cold launch never carries a
// sessionStorage token, so this page holds no auth logic of its own — AuthProvider's own
// bootstrap effect (AuthContext.tsx) is what actually redeems the refresh cookie for this exact
// path, picking tenant vs superadmin via the same last-known-kind hint this page reads for its
// fallback below. This component only waits for that to resolve and routes to where the result
// belongs, so the two never race each other issuing competing refresh calls.
export function PwaInit() {
  const navigate = useNavigate()
  const { user, isAuthenticated, isLoading } = useAuth()

  useEffect(() => {
    if (isLoading) {
      return
    }

    if (isAuthenticated && user) {
      navigate(getDashboardPath(user), { replace: true })
      return
    }

    // Refresh cookie was missing or expired — send them somewhere that can still get them
    // signed in without retyping a slug they've already used on this device.
    const lastTenantSlug = getPwaLastTenantSlug()
    if (lastTenantSlug) {
      navigate(`/${lastTenantSlug}/login`, { replace: true })
    } else if (getPwaAccountKind() === 'superadmin') {
      navigate('/superadmin/login', { replace: true })
    } else {
      navigate('/find-workspace', { replace: true })
    }
  }, [isLoading, isAuthenticated, user, navigate])

  return <AppBootScreen label="Opening your workspace..." />
}
