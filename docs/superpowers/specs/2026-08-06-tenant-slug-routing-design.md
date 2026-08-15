# Tenant URL Scheme: Subdomain → Path Slug, Dashboard + Public Booking Routing

## Context

Triggered by checking "what's the tenant dashboard URL?" before building the public booking page. Investigation found:

- The backend already has a self-service "requested subdomain" flow (`TenantRegistrationRequest.RequestedSubdomain`, validated by `ISubdomainValidationPolicy`, provisioned via `ProvisionTenantOnRequestApprovedHandler`) — tenant proposes it at signup, super admin's existing approval step is the natural checkpoint. This UX stays; only the underlying concept (subdomain → path slug) changes.
- `AppUrlService.GetTenantDashboardUrl`/`GetPaymentReturnUrl`/`GetPaymentCancelUrl`/`GetGuestCancellationUrl` are effectively dead code — the frontend never reads `ResetPasswordResult.RedirectUrl`, and `InitiateBookingHandler` never calls the return/cancel URL methods at all.
- The frontend's `AuthContext` derives `user` **entirely from decoding the JWT** (`buildUserFromToken`) — `login()`/`resetPassword()` discard their response bodies except the access token. So tenant identity reaching the app (for routing) must travel as a JWT claim, not a response-body field.
- `/app/booking` is hardcoded as a literal string in ~20 places across 11 frontend files, not centralized behind the one constant (`TENANT_DASHBOARD_PATH`) that already exists but most call sites don't use.
- Public API tenant resolution (`TenantMiddleware`'s anonymous branch) currently reads the request's Host-header subdomain. Per user decision, per-tenant DNS subdomains are unnecessary at this stage (single early customer, QR/direct-link traffic, simpler local dev) — moving to a path slug instead, following the standard early-SaaS guidance of centralizing tenant resolution behind one seam so subdomains/custom domains can be added later without a rewrite.

## Decisions (confirmed with user)

- Slug (not subdomain) is the tenant identifier, chosen by the tenant at signup, reviewed by super admin at approval (existing flow, renamed).
- No literal "app" or "localflow" URL segment. Slug is the first path segment for both areas:
  - Dashboard: `/<slug>/dashboard/...` (replaces `/app/booking/...`)
  - Public booking: `/<slug>/book` (new)
- Full retrofit now, not deferred — including centralizing the scattered `/app/booking` string behind one helper function.
- Dashboard gets a button to open the tenant's own public booking page (new tab), visible to all roles.

## Backend design (ApexBooking)

### 1. Rename `Subdomain` → `Slug`

| Before | After |
|---|---|
| `Tenant.Subdomain` (+ `Tenant.Create` param) | `Tenant.Slug` |
| `TenantRegistrationRequest.RequestedSubdomain` | `RequestedSlug` |
| `ISubdomainValidationPolicy` / `SubdomainValidationPolicy` | `ISlugValidationPolicy` / `SlugValidationPolicy` |
| `ITenantResolver.ResolveBySubdomainAsync` | `ResolveBySlugAsync` |
| `TenantResolver.cs` predicate `t.Subdomain == subdomain` | `t.Slug == slug` |
| `PendingTenantRequestSummary.RequestedSubdomain` | `RequestedSlug` |
| EF column `tenant.subdomain` | `tenant.slug` |
| EF column `tenant_registration_request.requested_subdomain` | `requested_slug` |
| `TenantRequestApproveDomainEvent.RequestedSubdomain` (and its one consumer in `ProvisionTenantOnRequestApprovedHandler`) | `RequestedSlug` |
| DI registration in `InfrastructureDependencies.cs`: `AddSingleton<ISubdomainValidationPolicy, SubdomainValidationPolicy>()` | `AddSingleton<ISlugValidationPolicy, SlugValidationPolicy>()` |

Validation rules in `SlugValidationPolicy` stay exactly as they are today (3-63 chars, lowercase alphanumeric + single internal hyphens, reserved-word blocklist `www/api/admin/administrator/billing/support/mail/portal/dev/staging`) — just renamed, no rule changes.

New EF Core migration required: rename both columns. (`dotnet ef migrations add RenameSubdomainToSlug` — left for manual run per your no-build/no-test-for-me instruction.)

### 2. New `tenant_slug` JWT claim

- `JwtClaimTypes.cs`: add `public const string TenantSlug = "tenant_slug";`
- `TokenDescriptor` / `TokenPrincipal` (`ITokenService.cs`): both gain `string? Slug`.
- `JwtTokenService.BuildClaims`: add the claim conditionally, same pattern as the existing `TenantId`/`Role` blocks:
  ```csharp
  if (descriptor.Slug is not null)
      claims.Add(new Claim(JwtClaimTypes.TenantSlug, descriptor.Slug));
  ```
- `JwtTokenService.MapPrincipal`: decode it back into `TokenPrincipal.Slug` for symmetry (used by `ValidateExpiredAccessToken`).
- Three call sites that build a `TokenDescriptor` thread `tenant.Slug` through:
  - `LoginHandler.cs` — tenant branch only (not the platform-admin branch, which has no tenant).
  - `ApplicationUserService.ResetPasswordAsync` — signature gains a `slug` parameter, threaded from `ResetPasswordHandler.cs` (which already resolves `tenant` via `TenantRepository.GetByUserIdAsync`).
  - `RefreshTokenHandler.cs` — already resolves `tenant` via `GetByUserIdAsync`; add `tenant.Slug` to the descriptor.

`BusinessProfileDto` (`GetBusinessProfileQuery.cs`) separately gains a `Slug` field — this is for the dashboard to read its own tenant's slug to build the "visit public booking page" link, not an auth concern.

### 3. Public API routing: Host-subdomain → path-slug

- `BookingsController.cs`: `[Route("api/public/bookings")]` → `[Route("api/public/{slug}/bookings")]`. Applies to the 5 wizard-step actions (branches/services/staff/availability/initiate). The `status/{ticketToken}` action (added last round) stays un-prefixed — it already intentionally resolves tenant from the signed ticket token, bypassing ambient tenant context entirely, so it's unaffected by this whole change.
- `TenantMiddleware.cs`: the anonymous branch drops `ExtractSubdomain(context.Request.Host.Host)` entirely and instead reads the matched route's `slug` value:
  ```csharp
  else
  {
      var slug = context.GetRouteValue("slug") as string;
      if (slug is not null)
      {
          var resolvedTenantId = await tenantResolver.ResolveBySlugAsync(slug, context.RequestAborted);
          if (resolvedTenantId is not null)
              tenantService.SetCurrentTenant(resolvedTenantId);
      }
  }
  ```
  Confirmed feasible: `Program.cs` calls `UseRouting()` before `UseMiddleware<TenantMiddleware>()`, so endpoint route values are already populated when this runs. The `ExtractSubdomain` helper method is deleted (no longer has any caller).
- Every existing public handler (`GetPublicBranchesHandler`, `GetPublicServicesByBranchHandler`, etc.) needs **no changes** — they only read the ambient `ITenantEntity.TenantId`, which is still populated the same way from their point of view; only the *source* of that resolution changes.
- **CORS**: the dev default `http://*.localhost:5096` (`CorsConfigurationExtensions.cs`) existed only to support subdomain-hosted anonymous traffic and is no longer meaningful — anonymous and authenticated traffic now come from the same flat SPA origin. Dev CORS needs to actually allow `http://localhost:5173` (the real Vite dev origin, per `FrontendBaseUrl`) instead. This appears to have been misconfigured/unused even before this change (the old default never matched port 5173 either) — flagging as a config fix bundled into this work, not scope creep.

## Frontend design (LocalFlow)

### 1. JWT decoding

- `utils/jwt.ts`: `IDecodedAccessToken` gains `slug: string`; `decodeJwt` reads the new `tenant_slug` claim key.
- `interfaces/IUser.ts`: gains `slug: string | null` (null for platform admin).
- `contexts/AuthContext.tsx`: `buildUserFromToken` maps the new field — no other changes needed here, this is the existing established pattern.

### 2. Centralized path helper

Replace the static `TENANT_DASHBOARD_PATH` constant in `config/dashboardRoutes.ts` with:

```ts
export function buildDashboardPath(slug: string, subPath = ''): string {
  return `/${slug}/dashboard${subPath ? `/${subPath}` : ''}`
}

export function buildPublicBookingPath(slug: string): string {
  return `/${slug}/book`
}
```

`getDashboardPath(user)` updates to call `buildDashboardPath(user.slug, ...)` for tenant roles (falls back to `/admin` unchanged for platform admin).

All ~20 hardcoded `/app/booking...` occurrences get updated to call `buildDashboardPath`/relative routing instead, across: `Topbar.tsx`, `modules.config.ts`, `booking.nav.config.ts`, `settings.nav.config.ts`, `SettingsLayout.tsx`, `LoginPage.tsx`, `BookingOverviewPage.tsx`, `BusinessProfilePage.tsx`, `AppRoutes.tsx`, `ProtectedRoute.tsx`. Each call site pulls `slug` from `useAuth().user.slug` (already authenticated at every one of these call sites, since they're all inside the protected dashboard tree).

### 3. Routing (`AppRoutes.tsx`)

- `/app` redirect and `/app/booking` route path both become `/:slug/dashboard`. Nested routes (`appointments`, `calendar`, `clients`, `staff`, `services`, `business-profile`, `branches`, `time-offs`, `settings/*`) keep their relative sub-paths unchanged — only the parent prefix moves.
- `ProtectedRoute.tsx`'s role-failure redirect (`<Navigate to="/app/booking" />`) becomes `<Navigate to={buildDashboardPath(user!.slug)} />`.
- New top-level route: `/:slug/book` → a placeholder page component for now (e.g. "Booking page coming soon"). The actual 5-step wizard UI is explicitly a separate design, as flagged at the end of last round's spec — this round only establishes the routing shell + slug plumbing it'll need.

### 4. `publicClient.ts` — simplifies

Drops the `window.location.hostname` subdomain-sniffing entirely (from last round). Becomes a plain axios instance with a fixed `baseURL` (same `VITE_API_BASE_URL` as `authClient`, since the API is one flat host now). Slug travels as an explicit path segment built by each service function, e.g. `getBranches(slug)` calls `/api/public/${slug}/bookings/branches`. The `VITE_PUBLIC_API_ROOT` env var added last round gets removed from `.env.local`/`.env.example` — no longer needed. `getBookingStatus(ticketToken)` is unaffected (already un-prefixed by design).

### 5. Dashboard button

Added to `Topbar.tsx`, linking to `buildPublicBookingPath(user.slug)`, `target="_blank"` (previewing/sharing your own public page, not in-app navigation). Visible to all three tenant roles — not a sensitive action.

### 6. Tenant self-service request + admin review (mechanical rename)

- `RequestAccessPage.tsx`: `subDomain` field/state/validation → `slug`; `SUBDOMAIN_PATTERN` → `SLUG_PATTERN` (same regex); label "Subdomain" → "Slug".
- `IRequestAccessFormValues.ts`: `subDomain` → `slug`.
- `TenantRequestDetailsModal.tsx`: "Requested Subdomain" label + `request.requestedSubdomain` → "Requested Slug" + `request.requestedSlug`.
- `ITenantRequest.ts`, `superAdminService.ts`: `requestedSubdomain` → `requestedSlug` (interface + wire mapping).

## Out of scope

- The actual 5-step public booking wizard UI (branch/service/staff/date/time/confirm screens) — separate design, next.
- Subdomain or custom-domain support — deliberately deferred per the user's chosen article's own staged approach; the tenant-resolution seam (`TenantMiddleware` + `ITenantResolver`) is what keeps that option open later without a rewrite.
- Removing the now-fully-dead `AppUrlService.GetTenantDashboardUrl`/`GetPaymentReturnUrl`/`GetPaymentCancelUrl`/`GetGuestCancellationUrl`/`GetPlatformDashboardUrl` methods — still unused after this change (their one former caller, `ResetPasswordResult.RedirectUrl`, remains dead weight since the frontend still won't read it), but cleaning up unrelated dead code isn't part of this request.

## Testing

- Backend: manual verification — submit a tenant request with a slug, approve it, confirm the provisioned tenant's `Slug` matches; log in, decode the returned JWT, confirm `tenant_slug` claim is present; hit `GET /api/public/{slug}/bookings/branches` for a real and a bogus slug (400/empty vs populated); confirm refresh-token flow preserves the claim.
- Frontend: manual — log in, confirm redirect lands on `/<slug>/dashboard`; click through sidebar nav and settings to confirm every link still resolves correctly under the new prefix; click the new public-booking button, confirm it opens `/<slug>/book` in a new tab; submit a tenant access request with a slug and confirm the super admin review screen shows it correctly.
