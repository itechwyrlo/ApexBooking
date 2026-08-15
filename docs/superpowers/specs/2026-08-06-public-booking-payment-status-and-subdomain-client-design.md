# Public Booking: Payment Status Polling + Subdomain-Aware API Client

## Context

Before building the customer-facing booking wizard UI, two gaps block a correct implementation, both identified while confirming the flow driven by `BookingsController.cs`'s five endpoints (Branches → Services → Staff → Availability → Initiate):

1. `InitiateBooking` returns `PayMongoQrCodeUrl`/`PayMongoCheckoutUrl` when payment is required, but payment confirmation lands asynchronously via `PayMongoWebhooksController` → `ProcessPaymentWebhookCommandHandler`, which flips `Booking.Status` from `PendingPayment` to `Scheduled` via `booking.ConfirmPayment(...)`. There is no public endpoint for the browser to ask "has this cleared yet?"
2. `TenantMiddleware` resolves the tenant for anonymous traffic purely from the request's Host header subdomain (`ExtractSubdomain`, skips `www`/`api`, requires ≥2 labels). LocalFlow's only existing API client (`authClient.ts`) uses a single flat `VITE_API_BASE_URL` with no subdomain awareness — correct for the authenticated dashboard (tenant comes from the JWT `tenant_id` claim there), wrong for the anonymous public booking page.

## Decisions (confirmed with user)

- Payment status detection: auto-poll, via a dedicated frontend hook, rather than a manual "I've paid" button.
- Local dev subdomain resolution: rely on `{subdomain}.localhost` auto-resolving to `127.0.0.1` (already anticipated by the backend's CORS dev default of `http://*.localhost:5096`) — no `TenantMiddleware` code changes.
- Public booking page's API client derives the tenant subdomain from the browser's own current hostname at runtime (the booking page is assumed served at `{tenant-subdomain}.yourapp.com/book`, mirroring the tenant's branded link) rather than a fixed env var or manual tenant selection.

## Backend design (ApexBooking)

### Query: `GetBookingStatusByTicketQuery`

`Features/PublicBookings/Queries/GetBookingStatusByTicket/GetBookingStatusByTicketQuery.cs`, same folder convention as `GetPublicBranches`/`GetPublicServicesByBranch`:

```csharp
public record PublicBookingStatusDto(
    Guid BookingId,
    string BookingReference,
    BookingStatus Status,
    string ServiceName,
    string StaffName,
    string BranchName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    bool RequiresUpfrontPayment,
    decimal AmountDue,
    string CurrencyCode
);

public record GetBookingStatusByTicketQuery(string TicketToken) : IQuery<PublicBookingStatusDto>;
```

`GetBookingStatusByTicketHandler`:
1. Calls `ITicketTokenService.TryValidate(query.TicketToken, out var payload)` (already exists, HMAC-signed and tamper-proof — `Issue`/`TryValidate` round-trip `BookingId`/`TenantId`/`BranchId`). Invalid/malformed token → `BusinessRuleBrokenException` (400).
2. Loads the tenant **directly by `payload.TenantId`** — deliberately bypasses `ITenantEntity`/`TenantMiddleware` subdomain resolution, since the token itself is the trusted source of tenant identity. This means the endpoint works regardless of which host/subdomain the browser lands on after the PayMongo redirect.
3. Finds the booking by `payload.BookingId` within `tenant.Bookings`; resolves `ServiceName`/`StaffName`/`BranchName` from `tenant.Services`/`tenant.Members`/`tenant.Branches` by the booking's `ServiceId`/`StaffId`/`BranchId`.
4. Not-found at any step → `BusinessRuleBrokenException`.

### Controller

New action on `BookingsController.cs` (class is already `[AllowAnonymous]`):

```csharp
[HttpGet("status/{ticketToken}")]
[ProducesResponseType(typeof(PublicBookingStatusDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GetBookingStatus([FromRoute] string ticketToken, CancellationToken cancellationToken)
{
    var result = await _mediator.Send(new GetBookingStatusByTicketQuery(ticketToken), cancellationToken);
    return Ok(result);
}
```

The ticket token's base64url + `.` alphabet is route-segment-safe (no slashes).

## Frontend design (LocalFlow)

### Public API client (`src/api/clients/publicClient.ts`)

```ts
import axios from 'axios'

function getSubdomain(): string | null {
  const labels = window.location.hostname.split('.')
  if (labels.length < 2) return null
  const candidate = labels[0].toLowerCase()
  return candidate === 'www' || candidate === 'api' ? null : candidate
}

export const publicClient = axios.create()

publicClient.interceptors.request.use((config) => {
  const subdomain = getSubdomain()
  if (!subdomain) {
    throw new Error('Unable to determine business from this URL.')
  }
  config.baseURL = `${window.location.protocol}//${subdomain}.${import.meta.env.VITE_PUBLIC_API_ROOT}`
  return config
})
```

`getSubdomain()` mirrors `TenantMiddleware.ExtractSubdomain` exactly (skip `www`/`api`, require ≥2 labels) so frontend and backend agree on what counts as a tenant subdomain. No `withCredentials`/auth interceptor — this client is anonymous by design, unlike `authClient`.

New env var: `VITE_PUBLIC_API_ROOT=localhost:5096` in `.env.local` for dev.

**Assumption flagged, not guessed**: production value of `VITE_PUBLIC_API_ROOT` and the exact prod domain/reverse-proxy layout (same-host vs. separate API subdomain) is unknown and out of scope here — this design only firms up the dev shape, which matches the backend's existing CORS default (`http://*.localhost:5096`) exactly. Needs your input at actual deploy time.

### Service (`src/services/publicBookingService.ts`)

Add `getBookingStatus(ticketToken: string): Promise<IPublicBookingStatus>` using `publicClient`. (The four existing wizard-step calls — branches/services/staff/availability/initiate — will live here too when the wizard itself is built; this spec only adds the status call needed for this slice.)

### Hook (`src/hooks/useBookingPaymentStatus.ts`)

```ts
interface IUseBookingPaymentStatusResult {
  status: IPublicBookingStatus | null
  isLoading: boolean
  error: string | null
}

export function useBookingPaymentStatus(ticketToken: string, intervalMs = 4000): IUseBookingPaymentStatusResult
```

- Fetches immediately on mount, then re-polls every `intervalMs` **only while** `status.status === 'PendingPayment'`.
- Stops polling automatically once status reaches any terminal state (`Scheduled`, `Cancelled`, etc.) or on unmount (clears the interval/timeout — no state updates after unmount).
- Uses recursive `setTimeout` (not `setInterval`) so a slow request can't overlap with the next poll tick.

## Out of scope

- The booking wizard screens themselves (branch/service/staff/date/time/confirm) — separate design, next.
- Production domain/reverse-proxy wiring for `VITE_PUBLIC_API_ROOT`.
- Any change to `TenantMiddleware` — the `{subdomain}.localhost` auto-resolution decision means no backend code changes are needed for local dev.

## Testing

- Backend: manual verification via Swagger/REST client — issue a booking via `POST /initiate`, confirm `GET /status/{ticketToken}` reflects `PendingPayment` (if payment required) or `Scheduled` (if not); simulate the PayMongo webhook and confirm status flips; confirm a tampered/garbage token 400s.
- Frontend: manual — run `npm run dev`, visit via a `{tenant}.localhost:5173` URL against a seeded tenant, confirm `publicClient` calls land on the matching `{tenant}.localhost:5096` origin; confirm the polling hook stops once status leaves `PendingPayment`.
