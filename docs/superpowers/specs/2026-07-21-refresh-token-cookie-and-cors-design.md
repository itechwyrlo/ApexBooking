# Refresh-token cookie tightening + dev CORS policy

## Context

`ICookieService` / `CookieService` already exist and are wired into the auth
handlers (`Login`, `RefreshToken`, `Logout`, and the SuperAdmin equivalents).
The refresh token is already cookie-only (HttpOnly, `SameSite=Strict`,
`Secure` driven by `Security:SecureCookies`, optional `Security:CookieDomain`).
The access token is intentionally never put in a cookie — it's short-lived
and returned only in the JSON response body.

CORS is also already implemented (`CorsConfigurationExtensions.cs`, policy
`ApplicationCorsPolicy`, registered before `UseAuthentication` in the
pipeline, `AllowCredentials: true`). The dev config just doesn't know about
the new frontend dev server yet.

## Goals

1. Make the refresh-token cookie's expiry configurable instead of hardcoded,
   and scope its `Path` so it isn't sent on every API request.
2. Allow the Vite dev frontend (`http://localhost:5173`) to call the API
   with credentials in development.

## Non-goals

- Changing the backend's own listening port (stays whatever it currently is
  in `launchSettings.json`).
- Touching `ITokenService` (currently mid-edit as part of an unrelated
  architecture refactor — out of scope).
- Touching prod/base appsettings, OAuth redirect URIs, or `FrontendBaseUrl`.

## Design

### 1. Cookie expiry becomes configurable

`SecurityOptions` (`ApexBooking.Infrastructure/Configuration/SecurityOptions.cs`)
gets a new property:

```csharp
public int RefreshTokenExpiryDays { get; set; } = 7;
```

`Program.cs` adds a second binding so this property picks up the existing
`Jwt:RefreshTokenExpiryDays` config key (already present in both
`appsettings.json` and `appsettings.Development.json` with value `7`, but
currently unused/unbound anywhere):

```csharp
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Jwt"));
```

Options binding is additive per matched property name — keys in the `Jwt`
section that don't match a `SecurityOptions` property (`Issuer`, `Audience`,
etc.) are ignored, so this only pulls in `RefreshTokenExpiryDays`.

`CookieService.BuildCookieOptions` uses `_securityOptions.RefreshTokenExpiryDays`
instead of the hardcoded `7`.

### 2. Cookie scoped to `/api/Auth`

`BuildCookieOptions` sets `Path = "/api/Auth"` explicitly (currently unset,
relying on browser default-path inference). All cookie-reading/writing
handlers live under the `AuthController` route (`api/Auth`), so this narrows
the cookie's exposure to only the endpoints that need it, without changing
behavior for those endpoints.

### 3. Dev CORS origin

`ApexBooking.WebApi/appsettings.Development.json`:

```json
"Cors": {
  "AllowedOrigins": "http://localhost:5173",
  ...
}
```

(replaces `"http://*.localhost:5096"`). `AllowCredentials` stays `true`
(already set) — required for the browser to send the HttpOnly cookie
cross-origin from `localhost:5173` to the API.

## Testing

- Build the solution to confirm no compile errors from the `SecurityOptions`
  change.
- Manual check (documented, not automated): login from a local request to
  the dev API, confirm `Set-Cookie: refreshToken=...; Path=/api/Auth;
  HttpOnly; SameSite=Strict` in the response, and that a subsequent
  `POST /api/Auth/refresh` from `http://localhost:5173` succeeds with
  `Access-Control-Allow-Origin: http://localhost:5173` and
  `Access-Control-Allow-Credentials: true` present.
