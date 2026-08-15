// Client-side routing hints only — never an authorization boundary. Both the interceptor
// (authRefreshInterceptor.ts) and the PWA cold-boot bootstrap (AuthContext.tsx, driven by
// PwaInit.tsx) use these to guess which of the two refresh cookies to try when there's no
// access token to decode and no `/admin` path prefix to infer from. Guessing wrong just means
// the wrong endpoint gets tried first and 401s — the httpOnly refresh cookie itself is what
// the backend actually authenticates against, in every case, regardless of what this says.
const PWA_ACCOUNT_KIND_STORAGE_KEY = 'pwa_account_kind'
const PWA_LAST_TENANT_SLUG_STORAGE_KEY = 'pwa_last_tenant_slug'

export type PwaAccountKind = 'superadmin' | 'tenant'

export function setPwaAccountKind(isPlatformAdmin: boolean): void {
  localStorage.setItem(PWA_ACCOUNT_KIND_STORAGE_KEY, isPlatformAdmin ? 'superadmin' : 'tenant')
}

export function getPwaAccountKind(): PwaAccountKind | null {
  const value = localStorage.getItem(PWA_ACCOUNT_KIND_STORAGE_KEY)
  return value === 'superadmin' || value === 'tenant' ? value : null
}

export function clearPwaAccountKind(): void {
  localStorage.removeItem(PWA_ACCOUNT_KIND_STORAGE_KEY)
}

// Lets PwaInit send a failed/expired-cookie boot straight to that tenant's own login page
// instead of a generic workspace finder, on a device that's already known to belong to one.
export function setPwaLastTenantSlug(slug: string): void {
  localStorage.setItem(PWA_LAST_TENANT_SLUG_STORAGE_KEY, slug)
}

export function getPwaLastTenantSlug(): string | null {
  return localStorage.getItem(PWA_LAST_TENANT_SLUG_STORAGE_KEY)
}

export function clearPwaLastTenantSlug(): void {
  localStorage.removeItem(PWA_LAST_TENANT_SLUG_STORAGE_KEY)
}
