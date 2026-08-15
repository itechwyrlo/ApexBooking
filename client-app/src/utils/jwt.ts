import type { Role } from '../types/Role'

export interface IDecodedAccessToken {
  sub: string
  email: string
  tenantId: string
  isPlatformAdmin: boolean
  roles: Role[]
  slug: string | null
  tenantMemberId: string | null
}

// Claim URIs/keys written by JwtTokenService.BuildClaims (ApexBooking.Core.Persistence)
const EMAIL_CLAIM_KEY = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
const ROLE_CLAIM_KEY = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
const TENANT_ID_CLAIM_KEY = 'tenant_id'
const PLATFORM_ADMIN_CLAIM_KEY = 'platform_admin'
const TENANT_SLUG_CLAIM_KEY = 'tenant_slug'
const TENANT_MEMBER_ID_CLAIM_KEY = 'tenant_member_id'

export function decodeJwt(token: string): IDecodedAccessToken {
  const payloadSegment = token.split('.')[1]
  const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
  const padding = (4 - (base64.length % 4)) % 4
  const padded = base64 + '='.repeat(padding)
  const json = decodeURIComponent(
    atob(padded)
      .split('')
      .map((char) => '%' + char.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  )
  const payload = JSON.parse(json) as Record<string, unknown>
  const rawRoles = payload[ROLE_CLAIM_KEY]
  const roles = Array.isArray(rawRoles) ? (rawRoles as Role[]) : rawRoles ? [rawRoles as Role] : []

  return {
    sub: String(payload.sub ?? ''),
    email: String(payload[EMAIL_CLAIM_KEY] ?? ''),
    tenantId: String(payload[TENANT_ID_CLAIM_KEY] ?? ''),
    isPlatformAdmin: payload[PLATFORM_ADMIN_CLAIM_KEY] === 'true' || payload[PLATFORM_ADMIN_CLAIM_KEY] === true,
    roles,
    slug: payload[TENANT_SLUG_CLAIM_KEY] ? String(payload[TENANT_SLUG_CLAIM_KEY]) : null,
    tenantMemberId: payload[TENANT_MEMBER_ID_CLAIM_KEY] ? String(payload[TENANT_MEMBER_ID_CLAIM_KEY]) : null,
  }
}
