# Environment Variables & Configuration Contract

This document defines all configuration values required by the ApexBooking application.

---

## Configuration Loading Order

ASP.NET Core loads configuration in this order (highest priority last):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (local development only)
4. Environment variables
5. Command-line arguments

---

## Naming Conventions

### User Secrets / appsettings.json
Uses JSON structure:
```json
{
  "Jwt": {
    "Issuer": "..."
  }
}
```

### Environment Variables
Uses double underscore (`__`) as separator:
```
Jwt__Issuer=...
```

---

## Required Configuration (CRITICAL)

These values **MUST** be set for the application to start.

### JWT Configuration

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `Jwt:Issuer` | string | JWT token issuer | `https://localhost:5127` |
| `Jwt:Audience` | string | JWT token audience | `https://localhost:5127` |
| `Jwt:PrivateKeyPem` | string | RSA private key (PEM format) | See note below |
| `Jwt:PublicKeyPem` | string | RSA public key (PEM format) | See note below |
| `Jwt:AccessTokenExpiryMinutes` | int | Access token lifetime | `15` |
| `Jwt:RefreshTokenExpiryDays` | int | Refresh token lifetime | `7` |

**Important**: RSA keys must be in PEM format with headers and footers.

---

## Database Configuration

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | string | SQL Server connection string | See below |

### Connection String Examples

**LocalDB (Development):**
```
Server=(localdb)\mssqllocaldb;Database=ApexBooking;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Full SQL Server:**
```
Server=YOUR_SERVER;Database=ApexBooking;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true
```

---

## Optional Configuration

### Email (Brevo SMTP)

| Key | Type | Description |
|-----|------|-------------|
| `BrevoSmtp:Key` | string | Brevo API key |
| `EmailSettings:SenderName` | string | Email sender name |
| `EmailSettings:SenderEmail` | string | Email sender address |

### Google Calendar Integration

| Key | Type | Description |
|-----|------|-------------|
| `GoogleCalendar:ClientId` | string | Google OAuth client ID |
| `GoogleCalendar:ClientSecret` | string | Google OAuth client secret |
| `GoogleCalendar:RedirectUri` | string | OAuth redirect URI |

### Microsoft Graph Integration

| Key | Type | Description |
|-----|------|-------------|
| `MicrosoftGraph:ClientId` | string | Microsoft OAuth client ID |
| `MicrosoftGraph:ClientSecret` | string | Microsoft OAuth client secret |
| `MicrosoftGraph:TenantId` | string | Microsoft tenant ID |
| `MicrosoftGraph:RedirectUri` | string | OAuth redirect URI |
| `MicrosoftGraph:SendAsMailbox` | string | Mailbox to send from |
| `MicrosoftGraph:Scopes` | array | OAuth scopes |

### Application URLs

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `ApplicationUrls:BaseUrl` | string | Application base URL | `https://localhost:5127` |
| `ApplicationUrls:StaffInviteAcceptPath` | string | Staff invite path | `/staff/accept-invite` |
| `AppSettings:BaseUrl` | string | Alternative base URL | `http://localhost:5096` |

### CORS Configuration

| Key | Type | Description |
|-----|------|-------------|
| `Cors:AllowedOrigins` | string | Comma-separated allowed origins |
| `Cors:AllowedMethods` | array | Allowed HTTP methods |
| `Cors:AllowedHeaders` | array | Allowed HTTP headers |
| `Cors:AllowCredentials` | bool | Allow credentials |
| `Cors:MaxAgeSeconds` | int | Preflight cache max age |

### Rate Limiting

| Key | Type | Description | Default |
|-----|------|-------------|---------|
| `RateLimiting:GlobalWindowSeconds` | int | Global window (seconds) | `60` |
| `RateLimiting:GlobalPermitLimit` | int | Global requests per window | `100` |
| `RateLimiting:AuthWindowMinutes` | int | Auth window (minutes) | `15` |
| `RateLimiting:AuthPermitLimit` | int | Auth attempts per window | `5` |

### Security

| Key | Type | Description | Default |
|-----|------|-------------|---------|
| `Security:RequireHttps` | bool | Enforce HTTPS | `true` |
| `Security:CookieDomain` | string | Cookie domain | `""` |
| `Security:EnableHsts` | bool | Enable HSTS | `true` |
| `Security:HstsMaxAgeSeconds` | int | HSTS max age | `31536000` |
| `Security:EnableContentTypeOptions` | bool | X-Content-Type-Options | `true` |
| `Security:EnableFrameOptions` | bool | X-Frame-Options | `true` |
| `Security:EnableCsp` | bool | Content-Security-Policy | `true` |
| `Security:CspPolicy` | string | CSP policy value | See appsettings.json |

---

## Environment-Specific Validation

### Development Environment
- ✅ Database connection string NOT validated (can use in-memory for testing)
- ✅ JWT keys STILL required (security first!)

### Production Environment
- ✅ ALL configuration validated
- ✅ Database connection REQUIRED
- ✅ HTTPS enforced by default

---

## Setting Values

### Local Development (User Secrets)

```bash
cd ApexBooking.WebApi
dotnet user-secrets set "Jwt:Issuer" "https://localhost:5127"
dotnet user-secrets set "Jwt:Audience" "https://localhost:5127"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Your-Connection-String"
```

### Environment Variables

**Windows (PowerShell):**
```powershell
$env:Jwt__Issuer="https://localhost:5127"
$env:Jwt__Audience="https://localhost:5127"
$env:ConnectionStrings__DefaultConnection="Your-Connection-String"
```

**Windows (Command Prompt):**
```cmd
set Jwt__Issuer=https://localhost:5127
set Jwt__Audience=https://localhost:5127
set ConnectionStrings__DefaultConnection=Your-Connection-String
```

**Linux/macOS:**
```bash
export Jwt__Issuer="https://localhost:5127"
export Jwt__Audience="https://localhost:5127"
export ConnectionStrings__DefaultConnection="Your-Connection-String"
```

---

## NEVER COMMIT

- ❌ `secrets.json`
- ❌ `appsettings.*.local.json`
- ❌ `.env.local`
- ❌ `.env.*.local`
- ❌ Any file containing private keys, passwords, or secrets

These are already protected in `.gitignore`.
