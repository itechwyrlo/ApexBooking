import type { IUser } from '../interfaces/IUser'

export const PLATFORM_ADMIN_DASHBOARD_PATH = '/admin'

// Tenant identity is a path slug, not a hostname/subdomain (see TenantMiddleware on the
// backend) — every tenant-scoped route starts with it. Centralized here so no call site
// hardcodes the shape; if the scheme ever changes again, this is the only place to edit.
export function buildDashboardPath(slug: string, subPath = ''): string {
  return `/${slug}/dashboard${subPath ? `/${subPath}` : ''}`
}

export function buildPublicBookingPath(slug: string): string {
  return `/${slug}/book`
}

// All three tenant-scoped roles land on the same shared dashboard today — there is no
// per-role landing page yet. isPlatformAdmin is the only branch; a future per-role dashboard
// only requires editing this function, not the call sites.
export function getDashboardPath(user: Pick<IUser, 'isPlatformAdmin' | 'slug'>): string {
  if (user.isPlatformAdmin) {
    return PLATFORM_ADMIN_DASHBOARD_PATH
  }

  // A tenant user without a slug claim means something upstream is broken (every tenant JWT
  // carries one). Login is slug-scoped now (/:slug/login) so there's no generic login path left
  // to fall back to — send them to the landing page instead of guessing a URL that can't resolve.
  return user.slug ? buildDashboardPath(user.slug) : '/'
}
