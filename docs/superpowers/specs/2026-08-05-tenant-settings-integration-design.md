# Tenant Settings Integration: Business Profile, Booking Policy, Payment Policy

## Context

`ApexBooking.WebApi/Controllers/TenantController.cs` already exposes three `PUT` endpoints for updating a tenant's business profile, booking policy, and payment policy:

- `PUT /api/Tenant/profile` → `UpdateBusinessProfileCommand`
- `PUT /api/Tenant/policy/booking` → `UpdateBookingPolicyCommand`
- `PUT /api/Tenant/policy/payment` → `UpdatePaymentPolicyCommand`

All three return `204 No Content` and none has a matching `GET` to fetch current values, and none has FluentValidation validators (the old ones were deleted in the in-progress refactor). On the frontend (`C:\Users\Wyrlo\projects\LocalFlow`, React 19 + Vite + TS + Bootstrap 5, axios-based service/hook pattern), the three corresponding settings pages exist only as routed `ModulePlaceholderPage` stubs — no forms, services, hooks, or interfaces exist yet.

This spec covers building the missing backend read side + validation, and the full frontend integration for these three settings areas.

## Goals

- Add `GET` endpoints so the frontend can pre-populate each settings form with current values.
- Add FluentValidation validators for the three existing commands, mirroring the domain rules already enforced inside the entities, so the API returns consistent field-level `400` errors.
- Build three real settings pages in LocalFlow (Business Profile, Booking Settings, Payment Settings) replacing the placeholder pages, following the codebase's existing service/hook/form conventions exactly.

## Out of scope

- Logo upload (no upload endpoint exists; `LogoUrl` field is dropped from the initial form).
- `BusinessType` editing (not supported by the command; shown read-only where relevant).
- Payment gateway credential configuration (`Tenant.ConfigurePaymentGateway` / `TenantPaymentCredential`) — no command/endpoint currently wires it up; unrelated to `PaymentPolicy`.
- Branch-level settings (all three policies are tenant-level, not per-branch).

## Backend design (ApexBooking)

### New queries + DTOs

Follow the existing `Features/Tenancy/Queries/GetAllTeam` / `GetAllBranches` pattern (query record → handler → DTO), same tenant-scoping approach as the `Update*Handler`s (`TenantRepository.GetAsync` with the matching `includes:`).

| Endpoint | Query / Handler | Response DTO fields |
|---|---|---|
| `GET /api/Tenant/profile` | `GetBusinessProfileQuery` / `GetBusinessProfileHandler` | `BusinessName`, `Description`, `LogoUrl`, `BusinessType` (read-only) |
| `GET /api/Tenant/policy/booking` | `GetBookingPolicyQuery` / `GetBookingPolicyHandler` | `BookingConfirmationMode`, `MinAdvanceBookingHours`, `MaxAdvanceBookingDays`, `CancellationCutoffHours`, `LateCancellationPolicy`, `GuestBookingEnabled`, `NotifyBookingConfirmed`, `NotifyBookingCancelled`, `NotifyBookingReminder`, `NotifyNewCustomer`, `ReminderHoursBefore` |
| `GET /api/Tenant/policy/payment` | `GetPaymentPolicyQuery` / `GetPaymentPolicyHandler` | `RequirementType`, `DepositType`, `DepositValue`, `RefundPercent` |

Each handler throws `NotFoundException`/`BusinessRuleBrokenException` if the tenant or the relevant policy navigation is null (matching existing handler behavior). DTOs live alongside the query in each feature folder, consistent with existing DTO placement (e.g. `TeamMemberSummary`, `BranchAdminSummary`).

Controller: add the three `[HttpGet]` actions to `TenantController.cs`, directly above their matching `[HttpPut]` counterparts. No new `[Authorize]` attribute needed — inherits class-level `ManagementOnly` policy.

### New FluentValidation validators

Added to `Common/Validators/`, picked up automatically by the existing `AddValidatorsFromAssembly` + `ValidationBehavior<,>` MediatR pipeline (`ApplicationServices.cs`) — no DI changes needed.

- `UpdateBusinessProfileCommandValidator`: `BusinessName` required, max length 200; `Description` max length 1000 when provided.
- `UpdateBookingPolicyCommandValidator`: `MinAdvanceBookingHours >= 0`; `MaxAdvanceBookingDays > 0`; `CancellationCutoffHours >= 0`; `ReminderHoursBefore >= 0`; `BookingConfirmationMode` and `LateCancellationPolicy` must be defined enum values.
- `UpdatePaymentPolicyCommandValidator`: `DepositValue >= 0`; when `DepositType == Percentage`, `DepositValue <= 100`; `RefundPercent` between 0 and 100 inclusive. Mirrors `PaymentPolicy.UpdatePolicy`'s domain rules exactly.

Validation failures flow through the existing `GlobalExceptionHandler`, producing `{ status, title, detail, errors: { fieldName: string[] } }` — the shape the frontend already parses (see `AddTeamMemberModal.tsx`).

## Frontend design (LocalFlow)

### Interfaces (`src/interfaces/`)

- `IBusinessProfile.ts`: `{ businessName: string; description: string | null; businessType: BusinessType }`
- `IBookingPolicy.ts`: `{ bookingConfirmationMode: BookingConfirmationMode; minAdvanceBookingHours: number; maxAdvanceBookingDays: number; cancellationCutoffHours: number; lateCancellationPolicy: CancellationPolicy; guestBookingEnabled: boolean; notifyBookingConfirmed: boolean; notifyBookingCancelled: boolean; notifyBookingReminder: boolean; notifyNewCustomer: boolean; reminderHoursBefore: number }`
- `IPaymentPolicy.ts`: `{ requirementType: PaymentRequirementType; depositType: DepositType; depositValue: number; refundPercent: number }`

### Type unions (`src/types/`)

`BookingConfirmationMode.ts` (`Automatic | Manual`), `CancellationPolicy.ts` (`NoRefund | PartialRefund | FullRefund`), `PaymentRequirementType.ts` (`None | DepositRequired | FullPaymentRequired`), `DepositType.ts` (`Percentage | FixedAmount`) — matching the existing `BusinessType.ts`/`Role.ts` string-union style (enums serialize as strings via the backend's global `JsonStringEnumConverter`).

### Services (`src/services/`)

One file per policy, each with a wire interface + mapper + `getX()`/`updateX(values)`, following `teamService.ts`:
- `businessProfileService.ts` → `GET`/`PUT /api/Tenant/profile`
- `bookingPolicyService.ts` → `GET`/`PUT /api/Tenant/policy/booking`
- `paymentPolicyService.ts` → `GET`/`PUT /api/Tenant/policy/payment`

### Hooks (`src/hooks/`)

`useBusinessProfile.ts`, `useBookingPolicy.ts`, `usePaymentPolicy.ts` — fetch-on-mount pattern matching `useBranches.ts`, each returning `{ data, isLoading, error, refetch }`.

### Pages

Replace the 3 `ModulePlaceholderPage` usages in `AppRoutes.tsx`:
- `src/pages/booking/BusinessProfilePage.tsx` → `/app/booking/business-profile`
- `src/pages/booking/settings/BookingSettingsPage.tsx` → `/app/booking/settings/booking`
- `src/pages/booking/settings/PaymentSettingsPage.tsx` → `/app/booking/settings/payment`

Each page: shows a loading skeleton while its hook fetches, then a form built from `FormGroup`/`Button`/`Card` with local `values`/`errors`/`touched`/`isSubmitting` state — the exact shape used by `AddTeamMemberModal.tsx`. Client-side `validate()` mirrors the backend validator rules (refund/deposit 0–100 bounds, required business name, non-negative windows). Submission calls the service's `updateX`, shows a success/error toast via `useToast` (parsing `error.response?.data?.detail` the same way `AddTeamMemberModal.tsx` does), and — since `PUT` returns `204` with no body — calls `refetch()` on success to reflect persisted state.

Booking/payment policy forms use Bootstrap `form-check` switches for boolean flags (`GuestBookingEnabled`, `Notify*`) and `form-select` for enum fields, consistent with existing form patterns.

`settings.nav.config.ts` requires no changes — it already points at the correct routes.

## Error handling

- Backend: validation errors → `400` with `errors` dict (FluentValidation); business-rule errors (e.g. deposit bounds enforced a second time in the domain layer) → `400` with a `detail` message but no per-field `errors`; not-found tenant/policy → `404`.
- Frontend: client-side validation blocks submission before any request is sent (fast feedback, matches existing modal pattern). Server-side errors surfaced via toast using `detail`; if `errors` is present, map field messages onto the corresponding form fields the same way `touched`/`errors` state already works.

## Testing

- Backend: no existing handler/controller test suite to extend (only `ApexBooking.Core.Domain.UnitTests` exists, covering domain events). New handlers are thin (load → map), so manual verification via a REST client (or Swagger) against a local run is the practical verification path here, consistent with the project's current test coverage level.
- Frontend: manual verification — run `npm run dev`, log in, navigate to each of the 3 settings pages, confirm current values load, submit valid and invalid changes, confirm toasts and persisted state after refetch/reload.
