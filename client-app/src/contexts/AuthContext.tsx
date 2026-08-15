import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { getAccessToken, clearAccessToken } from '../api/clients/authClient'
import { registerAuthRefreshInterceptor } from '../api/interceptors/authRefreshInterceptor'
import * as authService from '../services/authService'
import { decodeJwt } from '../utils/jwt'
import { getPwaAccountKind } from '../utils/pwaSessionHints'
import type { IUser } from '../interfaces/IUser'

registerAuthRefreshInterceptor()

interface IAuthContextValue {
  user: IUser | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (slug: string, email: string, password: string) => Promise<IUser>
  loginAsSuperAdmin: (email: string, password: string) => Promise<IUser>
  resetPassword: (
    userId: string,
    token: string,
    newPassword: string,
    confirmPassword: string,
  ) => Promise<IUser>
  logout: () => Promise<void>
}

const AuthContext = createContext<IAuthContextValue | undefined>(undefined)

// Pages whose entire purpose is to establish a brand-new session, plus the
// marketing landing page. Silently redeeming a leftover refresh cookie here
// races the explicit login submit against the backend's own refresh-token
// rotation for the same user row, which can trip EF's optimistic-concurrency
// check on the user entity — and the landing page never reads `user`/
// `isAuthenticated`, so it has no reason to bootstrap a session at all.
// request-access(/pending) are the same "never reads user/isAuthenticated"
// case as the landing page — no session-establishing call happens there,
// just no reason to bootstrap one either.
const PUBLIC_AUTH_PATHS = ['/', '/admin/login', '/find-workspace', '/request-access', '/request-access/pending']

// The customer-facing public booking page (/:slug/book) is fully anonymous, and so is the
// tenant login page itself (/:slug/login) — both have no business bootstrapping a leftover
// staff session from this browser, and doing so risks the same concurrency race noted above.
// The reset-password page (/:slug/reset-password) carries the identical risk — it calls
// useAuth().resetPassword, which establishes a brand-new session exactly like login does.
const PUBLIC_BOOKING_PATH_PATTERN = /^\/[^/]+\/book(\/.*)?$/
const TENANT_LOGIN_PATH_PATTERN = /^\/[^/]+\/login$/
const TENANT_RESET_PASSWORD_PATH_PATTERN = /^\/[^/]+\/reset-password$/

// The PWA's start_url (see vite.config.ts). Deliberately NOT in PUBLIC_AUTH_PATHS above — unlike
// those pages, PwaInit.tsx's entire purpose is reading `user`/`isAuthenticated` to decide where
// to route, so the bootstrap effect below must still fire here. It only gets a dedicated branch
// because neither of that effect's existing signals for "which cookie to try" apply: there's no
// stored token to decode, and the path carries no /admin prefix to infer from either.
const PWA_INIT_PATH = '/pwa-init'

function isAnonymousPath(pathname: string): boolean {
  return (
    PUBLIC_AUTH_PATHS.includes(pathname) ||
    PUBLIC_BOOKING_PATH_PATTERN.test(pathname) ||
    TENANT_LOGIN_PATH_PATTERN.test(pathname) ||
    TENANT_RESET_PASSWORD_PATH_PATTERN.test(pathname)
  )
}

function buildUserFromToken(token: string): IUser {
  const decoded = decodeJwt(token)
  return {
    id: decoded.sub,
    email: decoded.email,
    tenantId: decoded.tenantId,
    isPlatformAdmin: decoded.isPlatformAdmin,
    roles: decoded.roles,
    slug: decoded.slug,
    tenantMemberId: decoded.tenantMemberId,
  }
}

interface IAuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: IAuthProviderProps) {
  const [user, setUser] = useState<IUser | null>(() => {
    const token = getAccessToken()
    return token ? buildUserFromToken(token) : null
  })
  // Only bootstraps via the refresh cookie when no access token survived in this tab's
  // sessionStorage (e.g. a fresh tab) — an existing token is trusted until an API call 401s.
  const [isLoading, setIsLoading] = useState(() => getAccessToken() === null)

  useEffect(() => {
    const pathname = window.location.pathname
    if (getAccessToken() !== null || isAnonymousPath(pathname)) {
      setIsLoading(false)
      return
    }

    let isMounted = true

    // No stored token to decode yet (fresh tab) — infer session kind some other way. Everywhere
    // except PWA_INIT_PATH that's the route itself (same signal isAnonymousPath above already
    // relies on): superadmin lives under /admin, everything else that reaches here is a tenant
    // route. /pwa-init carries no such prefix — it's one shared launch URL for both account
    // kinds — so it falls back to the last-known-kind hint from login/refresh instead; see
    // pwaSessionHints.ts for why a stale or missing hint here is harmless.
    const bootstrapRefresh =
      pathname === PWA_INIT_PATH
        ? getPwaAccountKind() === 'superadmin'
          ? authService.refreshSuperAdminToken
          : authService.refreshToken
        : pathname.startsWith('/admin')
          ? authService.refreshSuperAdminToken
          : authService.refreshToken

    bootstrapRefresh()
      .then(() => {
        if (!isMounted) return
        const token = getAccessToken()
        setUser(token ? buildUserFromToken(token) : null)
      })
      .catch(() => {
        if (!isMounted) return
        setUser(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [])

  function establishSessionFromStoredToken(failureMessage: string): IUser {
    const token = getAccessToken()
    const authenticatedUser = token ? buildUserFromToken(token) : null
    setUser(authenticatedUser)
    if (!authenticatedUser) {
      throw new Error(failureMessage)
    }
    return authenticatedUser
  }

  async function login(slug: string, email: string, password: string): Promise<IUser> {
    await authService.login(slug, email, password)
    return establishSessionFromStoredToken('Login succeeded but no access token was issued.')
  }

  async function loginAsSuperAdmin(email: string, password: string): Promise<IUser> {
    await authService.loginAsSuperAdmin(email, password)
    return establishSessionFromStoredToken('Login succeeded but no access token was issued.')
  }

  async function resetPassword(
    userId: string,
    token: string,
    newPassword: string,
    confirmPassword: string,
  ): Promise<IUser> {
    await authService.resetPassword(userId, token, newPassword, confirmPassword)
    return establishSessionFromStoredToken('Password was reset but no access token was issued.')
  }

  async function logout(): Promise<void> {
    await authService.logout()
    clearAccessToken()
    setUser(null)
  }

  const value: IAuthContextValue = {
    user,
    isAuthenticated: user !== null,
    isLoading,
    login,
    loginAsSuperAdmin,
    resetPassword,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): IAuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
