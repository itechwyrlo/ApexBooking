# Refresh-Token Cookie Scoping: Separate Tenant / Superadmin Sessions

## Context

`ICookieService` ([ICookieService.cs](../../../ApexBooking.Core.Domain/Services/Cookie/ICookieService.cs))
writes a single, unnamed-by-role cookie — literally `"refreshToken"` — regardless of who's
logging in. `TenantLoginHandler.cs:72`, `PlatformAdminLoginHandler.cs:44`, and
`ResetPasswordHandler.cs:53` all call the exact same `SetRefreshTokenCookie(token)`.
Cookies are shared across every tab of the same browser profile (unlike `sessionStorage`,
which is genuinely per-tab) — so logging in as a platform admin in one tab silently
overwrites the tenant session's refresh cookie in every other tab.

Reproduced in practice: Tab A logs into a tenant (`supremo-barber`); Tab B logs in as
superadmin, overwriting the shared cookie. Tab A's `sessionStorage` access token is
untouched at that point and still looks like a valid tenant session — but the next time
Tab A's token expires (15 min) or any request 401s, `authRefreshInterceptor.ts` silently
calls `/api/auth/refresh`, which reads the now-superadmin cookie and hands back a valid
**superadmin** access token. Tab A silently becomes a superadmin session with no tenant
claim, and every tenant-scoped API call it makes afterward fails with "No authenticated
tenant context was found" (400, via `BusinessRuleBrokenException` →
[GlobalExceptionHandler.cs:38-41](../../../ApexBooking.WebApi/Infrastructure/GlobalExceptionHandler.cs#L38-L41)) —
`TenantMiddleware.cs:19-24` deliberately bypasses tenant resolution for platform admins,
so nothing sets a tenant context, and every downstream tenant-scoped handler breaks
identically.

## Decisions (confirmed with user)

- Fix the root cause (cookie name collision), not the downstream symptom. Two distinct
  cookies, one per session kind, so a superadmin login in one tab can never touch a
  tenant session's cookie in another.
- Mirrors the existing login split (`/api/{slug}/auth/login` vs
  `/api/superadmin/auth/login` are already two distinct endpoints) rather than inventing
  a new pattern — refresh gets the same treatment.
- `RotateRefreshTokenAsync`'s actual rotation/security logic (hash comparison,
  replay-family revocation) is untouched — this is purely about which cookie delivers the
  secret to that logic, not the logic itself.
- Out of scope: `ProtectedRoute.tsx` currently doesn't verify the logged-in user's `slug`
  matches the route's `:slug` param — a related but separate hardening opportunity,
  flagged but not part of this fix.

## Backend design (ApexBooking)

### `ICookieService`

```csharp
public interface ICookieService
{
    string GetRefreshTokenFromCookie(bool isPlatformAdmin);
    void SetRefreshTokenCookie(string refreshToken, bool isPlatformAdmin);
    void DeleteRefreshTokenCookie(bool isPlatformAdmin);
}
```

`CookieService.cs` picks the cookie name: `isPlatformAdmin ? "superadminRefreshToken" : "refreshToken"`.
Everything else about `BuildCookieOptions` (HttpOnly, `SameSite=Strict`, 7-day expiry, the
existing `Secure`/`Domain` config) is unchanged.

### Call sites

- `TenantLoginHandler.cs:72` → `SetRefreshTokenCookie(refreshTokenRaw, isPlatformAdmin: false)`.
- `PlatformAdminLoginHandler.cs:44` → `SetRefreshTokenCookie(refreshTokenRaw, isPlatformAdmin: true)`.
- `ResetPasswordHandler.cs:53` (tenant-only flow) → `SetRefreshTokenCookie(session.RefreshToken, isPlatformAdmin: false)`.
- `RefreshTokenHandler.cs` — needs to know which cookie to read *before* it knows who the
  user is (that's what it's about to determine). `RefreshTokenCommand` gains
  `IsPlatformAdmin`, set by which controller route was hit:
  ```csharp
  public record RefreshTokenCommand(bool IsPlatformAdmin) : ICommand<RefreshTokenResponse>;
  ```
  `Handle` reads `_cookieService.GetRefreshTokenFromCookie(command.IsPlatformAdmin)`, and
  after a successful rotation, writes back via
  `_cookieService.SetRefreshTokenCookie(rotation.RefreshTokenSecret, command.IsPlatformAdmin)`.
  Defense-in-depth check added right after resolving `rotation`: if
  `rotation.IsPlatformAdmin != command.IsPlatformAdmin`, treat it the same as any other
  invalid-secret case (`Reject()` — revoke-and-401) rather than issuing a token for a
  different kind of account than the calling route implies. Shouldn't trigger post-cutover
  (each cookie can now only ever hold the right kind of secret going forward), but it's a
  free correctness guard against a stale mixed-shape cookie during the cutover window.
- `LogoutHandler.cs` — needs to delete the *right* cookie for whoever's currently logged
  in. `IUserContextService` gains `bool IsPlatformAdmin()`
  ([IUserContextService.cs](../../../ApexBooking.Core.Domain/Interfaces/IUserContextService.cs)),
  implemented in `UserContextService.cs` by checking the `platform_admin` claim — the same
  check `TenantMiddleware.cs:17` already does. `LogoutCommand` itself is unchanged (no new
  field needed); `LogoutHandler.Handle` calls
  `_cookieService.DeleteRefreshTokenCookie(_userContext.IsPlatformAdmin())`.

### `AuthController.cs`

The existing action becomes explicitly the tenant path:

```csharp
[HttpPost("refresh")]
[AllowAnonymous]
public async Task<IActionResult> Refresh(CancellationToken ct)
{
    var result = await _mediator.Send(new RefreshTokenCommand(IsPlatformAdmin: false), ct);
    return Ok(result);
}
```

New action, absolute route matching the existing `/api/superadmin/auth/login`
([AuthController.cs:57](../../../ApexBooking.WebApi/Controllers/AuthController.cs#L57))
convention:

```csharp
[HttpPost("/api/superadmin/auth/refresh")]
[AllowAnonymous]
public async Task<IActionResult> RefreshSuperAdmin(CancellationToken ct)
{
    var result = await _mediator.Send(new RefreshTokenCommand(IsPlatformAdmin: true), ct);
    return Ok(result);
}
```

`logout` ([AuthController.cs:95-100](../../../ApexBooking.WebApi/Controllers/AuthController.cs#L95-L100)) is unchanged — no new route, no command change.

### Cutover

Tenant sessions are unaffected (cookie name unchanged). Any platform admin mid-session
when this deploys needs to log in once more — their existing cookie is still named
`"refreshToken"` under the old scheme, which post-deploy is the *tenant* cookie name, so
`GetRefreshTokenFromCookie(isPlatformAdmin: true)` won't find it (it's looking for
`"superadminRefreshToken"`). One-time, minor, not worth engineering a migration path for.

## Frontend design (LocalFlow)

### `services/authService.ts`

Split the existing `refreshToken()` into two:

```ts
export async function refreshToken(): Promise<void> {
  const response = await authClient.post<IRefreshResponse>('/api/auth/refresh')
  setAccessToken(response.data.accessToken)
}

export async function refreshSuperAdminToken(): Promise<void> {
  const response = await authClient.post<IRefreshResponse>('/api/superadmin/auth/refresh')
  setAccessToken(response.data.accessToken)
}
```

### `api/interceptors/authRefreshInterceptor.ts`

On a 401, decode the *currently stored* (now-stale) access token to determine which
refresh to call:

```ts
import { decodeJwt } from '../../utils/jwt'

function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    const currentToken = getAccessToken()
    const isPlatformAdmin = currentToken ? decodeJwt(currentToken).isPlatformAdmin : false
    const refresh = isPlatformAdmin ? refreshSuperAdminToken : refreshToken

    refreshPromise = refresh()
      .then(() => getAccessToken()!)
      .finally(() => { refreshPromise = null })
  }
  return refreshPromise
}
```

The existing `isRefreshCall = config?.url?.includes('/refresh')` guard (skips re-triggering
the interceptor for the refresh call's own response) already covers both
`/api/auth/refresh` and `/api/superadmin/auth/refresh` via the shared `/refresh` substring
— no change needed there.

### `contexts/AuthContext.tsx`

The bootstrap effect (fresh tab, no access token yet to decode) can't use the interceptor's
approach — it infers session kind from the **route** instead, same pattern already used
right there for `isAnonymousPath`:

```ts
const refresh = window.location.pathname.startsWith('/admin')
  ? authService.refreshSuperAdminToken
  : authService.refreshToken

refresh()
  .then(() => { /* unchanged */ })
  .catch(() => { /* unchanged */ })
```

`logout()` is unchanged — the backend now resolves the correct cookie from the JWT itself,
no frontend change needed.

## Non-goals

No change to `RotateRefreshTokenAsync`'s rotation/replay-detection logic. No change to
access-token shape/claims. No change to `ProtectedRoute.tsx`'s route-guard gap (flagged,
separate follow-up). No migration tooling for platform admins' pre-existing cookies —
one-time re-login is accepted.

## Testing

- Two tabs, same browser: log into a tenant in Tab A, log into superadmin in Tab B, wait
  for (or force) Tab A's access token to expire, confirm Tab A's next request triggers a
  *tenant* refresh and keeps working — not a silent identity swap.
- `RefreshTokenHandler`: cookie under the tenant name, `IsPlatformAdmin: true` command →
  rejected (mismatch guard), not silently honored.
- Logout from a superadmin session deletes `superadminRefreshToken`, not `refreshToken`,
  and vice versa.
- Fresh tab, no stored access token, navigating directly to `/admin/...` → bootstrap calls
  the superadmin refresh endpoint, not the tenant one.
