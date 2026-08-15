import axios from 'axios'
import { authClient, setAccessToken, clearAccessToken } from '../api/clients/authClient'
import { decodeJwt } from '../utils/jwt'
import {
  setPwaAccountKind,
  setPwaLastTenantSlug,
  clearPwaAccountKind,
  clearPwaLastTenantSlug,
} from '../utils/pwaSessionHints'
import type { IRequestAccessFormValues } from '../interfaces/IRequestAccessFormValues'

interface ILoginResponse {
  token: string
  email: string
  fullName: string
  isPlatformAdmin: boolean
  tenantId?: string
  tenantRole?: string
}

interface IRefreshResponse {
  accessToken: string
}

interface IResetPasswordResponse {
  accessToken: string
  redirectUrl: string
  email: string
  fullName: string
}

interface IFindWorkspaceResponse {
  slug: string | null
}

export interface IWorkspaceSummary {
  businessName: string
  logo: string | null
}

// Persists which cookie this browser just authenticated with, purely so a cold PWA launch
// (src/pages/PwaInit.tsx — sessionStorage never survives an app restart) knows which refresh
// endpoint to try first next time. Not a security boundary; see pwaSessionHints.ts.
function recordPwaSessionHints(token: string): void {
  const decoded = decodeJwt(token)
  setPwaAccountKind(decoded.isPlatformAdmin)
  if (decoded.slug) {
    setPwaLastTenantSlug(decoded.slug)
  }
}

export async function login(slug: string, email: string, password: string): Promise<void> {
  const response = await authClient.post<ILoginResponse>(`/api/${slug}/auth/login`, { email, password })
  setAccessToken(response.data.token)
  recordPwaSessionHints(response.data.token)
}

export async function loginAsSuperAdmin(email: string, password: string): Promise<void> {
  const response = await authClient.post<ILoginResponse>('/api/superadmin/auth/login', { email, password })
  setAccessToken(response.data.token)
  recordPwaSessionHints(response.data.token)
}

// Never rejects on "not found" — a null slug is a normal, expected outcome (unknown email,
// platform admin, or inactive tenant all look identical from here), not an error.
export async function findWorkspace(email: string): Promise<string | null> {
  const response = await authClient.post<IFindWorkspaceResponse>('/api/auth/find-workspace', { email })
  return response.data.slug
}

// Returns null when the slug doesn't resolve to a real, active workspace — a bad slug reached via
// a direct URL (/:slug/login) is an expected outcome here, not an error, mirroring findWorkspace's
// own "no throw on not-found" shape.
export async function getWorkspaceSummary(slug: string): Promise<IWorkspaceSummary | null> {
  try {
    const response = await authClient.get<IWorkspaceSummary>(`/api/${slug}/workspace-summary`)
    return response.data
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return null
    }
    throw error
  }
}

export async function resetPassword(
  userId: string,
  token: string,
  newPassword: string,
  confirmPassword: string,
): Promise<void> {
  const response = await authClient.post<IResetPasswordResponse>('/api/auth/reset-password', {
    userId,
    token,
    newPassword,
    confirmPassword,
  })
  setAccessToken(response.data.accessToken)
}

export async function refreshToken(): Promise<void> {
  const response = await authClient.post<IRefreshResponse>('/api/auth/refresh')
  setAccessToken(response.data.accessToken)
  recordPwaSessionHints(response.data.accessToken)
}

// Separate endpoint, separate cookie ("superadminRefreshToken" vs "refreshToken") — a
// superadmin session refreshing must never read/rotate a tenant session's cookie, or vice
// versa, even when both are open in different tabs of the same browser.
export async function refreshSuperAdminToken(): Promise<void> {
  const response = await authClient.post<IRefreshResponse>('/api/superadmin/auth/refresh')
  setAccessToken(response.data.accessToken)
  recordPwaSessionHints(response.data.accessToken)
}

export async function logout(): Promise<void> {
  await authClient.post('/api/auth/logout')
  clearAccessToken()
  clearPwaAccountKind()
  clearPwaLastTenantSlug()
}

export async function requestAccess(values: IRequestAccessFormValues): Promise<void> {
  await authClient.post('/api/TenantRequests/request-access', values)
}
