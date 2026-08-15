import type { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { authClient, getAccessToken, setAccessToken, clearAccessToken } from '../clients/authClient'
import { refreshToken, refreshSuperAdminToken } from '../../services/authService'
import { decodeJwt } from '../../utils/jwt'
import { getPwaAccountKind, getPwaLastTenantSlug } from '../../utils/pwaSessionHints'

interface IRetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

let refreshPromise: Promise<string> | null = null
let hasRedirectedToLogin = false

// A mid-session refresh failure (installed PWA left open past both token lifetimes, cookie
// revoked, etc.) previously only cleared the access token — AuthContext.user stayed stale and
// nothing sent the tenant back to a login screen. A full navigation is used rather than reaching
// into AuthContext from this plain axios module: landing on /:slug/login is already one of
// AuthContext's anonymous paths, so its bootstrap effect won't try to refresh again on load (no
// loop), and the fresh page load naturally resets `user` to null. hasRedirectedToLogin just stops
// multiple in-flight requests that all 401 at once from each queuing their own navigation.
function redirectToLoginAfterFailedRefresh(preRefreshToken: string | null): void {
  if (hasRedirectedToLogin) {
    return
  }

  const isPlatformAdmin = preRefreshToken ? decodeJwt(preRefreshToken).isPlatformAdmin : getPwaAccountKind() === 'superadmin'
  const slug = preRefreshToken ? decodeJwt(preRefreshToken).slug : getPwaLastTenantSlug()
  const destination = isPlatformAdmin ? '/superadmin/login' : slug ? `/${slug}/login` : '/find-workspace'

  if (window.location.pathname === destination) {
    return
  }

  hasRedirectedToLogin = true
  window.location.assign(destination)
}

function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    // Decode the currently-stored (now-stale) token purely to learn which kind of session
    // this tab last had — not to validate it, expiry/signature aren't checked here. That
    // decides which of the two independent refresh cookies gets rotated, so a superadmin
    // session refreshing in one tab can never touch a tenant session's cookie in another.
    //
    // A PWA launch has no stored token at all (sessionStorage never survives an app restart),
    // which used to fall straight through to `false` here and always retry as a tenant — wrong
    // for platform admins. Fall back to the last-known-kind hint from login/refresh instead;
    // see pwaSessionHints.ts for why that's safe to get wrong.
    const currentToken = getAccessToken()
    const isPlatformAdmin = currentToken
      ? decodeJwt(currentToken).isPlatformAdmin
      : getPwaAccountKind() === 'superadmin'
    const refresh = isPlatformAdmin ? refreshSuperAdminToken : refreshToken

    refreshPromise = refresh()
      .then(() => getAccessToken()!)
      .finally(() => {
        refreshPromise = null
      })
  }
  return refreshPromise
}

export function registerAuthRefreshInterceptor(): void {
  authClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const config = error.config as IRetryableRequestConfig | undefined
      const isRefreshCall = config?.url?.includes('/refresh')

      if (error.response?.status !== 401 || !config || config._retry || isRefreshCall) {
        return Promise.reject(error)
      }

      config._retry = true
      const preRefreshToken = getAccessToken()

      try {
        const newAccessToken = await refreshAccessToken()
        setAccessToken(newAccessToken)
        return authClient(config)
      } catch (refreshError) {
        clearAccessToken()
        redirectToLoginAfterFailedRefresh(preRefreshToken)
        return Promise.reject(refreshError)
      }
    },
  )
}
