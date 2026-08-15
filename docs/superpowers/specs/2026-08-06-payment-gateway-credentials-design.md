# Payment Gateway (PayMongo) Credentials: Owner Settings

## Context

`Tenant.ConfigurePaymentGateway(secretKey, publicKey)` already exists on the aggregate root ([Tenant.cs:488-499](../../../ApexBooking.Core.Domain/Entities/Tenant.cs#L488-L499)), backed by `TenantPaymentCredential` ([TenantPaymentCredential.cs](../../../ApexBooking.Core.Domain/Entities/TenantPaymentCredential.cs)) with `SecretKey`, `PublicKey`, `IsEnabled`, `CreatedAt`, `UpdatedAt`. Domain-level validation already enforces PayMongo's key prefixes (`sk_test_`/`sk_live_`, `pk_test_`/`pk_live_`).

No Application-layer command, handler, or controller endpoint calls this method anywhere — the only Application-layer reference to `PaymentCredential` is `InitiateBookingHandler` reading `SecretKey`/`IsEnabled` at checkout time. The frontend (LocalFlow) has zero references to payment credentials. This was explicitly called out as out-of-scope in the prior [2026-08-05-tenant-settings-integration-design.md](2026-08-05-tenant-settings-integration-design.md) spec, which covered Business Profile / Booking Policy / Payment Policy only.

This spec covers the missing vertical slice: configure/view PayMongo credentials, surfaced as a new section on the existing `PaymentSettingsPage.tsx`.

## Decisions (confirmed with user)

- Lives as a new "Payment Gateway" card on the existing `PaymentSettingsPage.tsx`, not a separate page/route.
- Restricted to the Owner role only (financial credentials), not the controller's default `ManagementOnly` (Owner+Admin).
- GET returns the Public Key in full (safe to display, it's the publishable key) plus a masked hint of the Secret Key and derived Test/Live mode — never the raw secret key.
- No enable/disable toggle in this slice — configure/update only. `TenantPaymentCredential.Disable()` stays unused for now.

## Backend design (ApexBooking)

### Command

`Features/Tenancy/Commands/PaymentGateway/ConfigurePaymentGatewayCommand.cs`:

```csharp
public record ConfigurePaymentGatewayCommand(string SecretKey, string PublicKey) : ICommand;
```

`ConfigurePaymentGatewayHandler`: same shape as `UpdatePaymentPolicyHandler` — resolve `_tenantEntity.TenantId`, load tenant via `_unitOfWork.TenantRepository.GetAsync(predicate: t => t.TenantId == tenantId, includes: t => t.PaymentCredential!)`, call `tenant.ConfigurePaymentGateway(command.SecretKey, command.PublicKey)` (the domain method already branches create-vs-update internally), `_unitOfWork.TenantRepository.Update(tenant)`, `CompleteAsync`. Unlike `UpdatePaymentPolicyHandler`, does **not** throw when `tenant.PaymentCredential` is null going in — that's the expected first-time-configuration state.

### Validator

`Common/Validators/ConfigurePaymentGatewayCommandValidator.cs`, picked up automatically by the existing pipeline:

- `SecretKey`: required; must start with `sk_test_` or `sk_live_`.
- `PublicKey`: required; must start with `pk_test_` or `pk_live_`.

Mirrors the domain's own check in `TenantPaymentCredential.UpdateCredentials` so bad input 400s with field-level errors before reaching the domain layer.

### Query

`Features/Tenancy/Queries/GetPaymentGatewayCredential/GetPaymentGatewayCredentialQuery.cs`:

```csharp
public record PaymentGatewayCredentialDto(string? PublicKey, string? MaskedSecretKey, string? Mode, bool IsConfigured);
public record GetPaymentGatewayCredentialQuery() : IQuery<PaymentGatewayCredentialDto>;
```

`GetPaymentGatewayCredentialHandler`: loads tenant with `includes: t => t.PaymentCredential!`. If `tenant.PaymentCredential` is null, returns `new PaymentGatewayCredentialDto(null, null, null, false)` — no exception, this is a valid "not connected yet" state (unlike `PaymentPolicy`, which is guaranteed non-null since it's seeded on tenant creation). Otherwise:

- `Mode`: `credential.SecretKey.StartsWith("sk_live_") ? "Live" : "Test"`.
- `MaskedSecretKey`: 8-char prefix + `"••••"` + last 4 characters of `SecretKey` (e.g. `sk_live_••••4f2a`). Computed at read time only, never persisted.
- `PublicKey`: returned in full.
- `IsConfigured`: `true`.

### Controller

Two new actions on `TenantController.cs`, placed next to the existing `policy/payment` pair:

```csharp
[HttpGet("payment-gateway")]
[Authorize(Roles = "Owner")]
[ProducesResponseType(typeof(PaymentGatewayCredentialDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetPaymentGatewayCredential() { ... }

[HttpPut("payment-gateway")]
[Authorize(Roles = "Owner")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ConfigurePaymentGateway([FromBody] ConfigurePaymentGatewayCommand command) { ... }
```

Both override the class-level `ManagementOnly` policy, same pattern already used for `ApproveTimeOff`/`RejectTimeOff`.

## Frontend design (LocalFlow)

### Interface (`src/interfaces/IPaymentGatewayCredential.ts`)

```ts
interface IPaymentGatewayCredential {
  publicKey: string | null
  maskedSecretKey: string | null
  mode: 'Test' | 'Live' | null
  isConfigured: boolean
}
```

### Service (`src/services/paymentGatewayService.ts`)

`getPaymentGatewayCredential()` (`GET /api/Tenant/payment-gateway`) and `configurePaymentGateway({ secretKey, publicKey })` (`PUT /api/Tenant/payment-gateway`), following the wire-interface + mapper pattern in `paymentPolicyService.ts`.

### Hook (`src/hooks/usePaymentGatewayCredential.ts`)

Fetch-on-mount, same shape as `usePaymentPolicy.ts`: `{ credential, isLoading, error, refetch }`.

### UI

New `Card` section added to `PaymentSettingsPage.tsx`, titled "Payment Gateway (PayMongo)", as an independent form with its own `values`/`errors`/`isSubmitting` state (separate from the existing Payment Policy form on the same page — different endpoint, different submit lifecycle):

- Status line: when `isConfigured`, show the `mode` as a badge (Test/Live) and `maskedSecretKey`; when not configured, show "Not connected."
- `Public Key` text input, prefilled with `credential.publicKey` when configured.
- `Secret Key` password-style input, **always starts blank** regardless of configured state — `ConfigurePaymentGateway` requires both keys together with no partial-update path domain-side, so changing either field means re-submitting both. Inline help text: "Re-enter your secret key to update either value."
- Client-side validation mirrors the backend validator (prefix checks) before submit.
- Submit calls `configurePaymentGateway`, shows a success/error toast via `useToast` (parsing `error.response?.data?.detail`/`errors` the same way `AddTeamMemberModal.tsx` does), calls `refetch()` on success (endpoint returns `204`).
- **Role gating**: `PaymentSettingsPage.tsx` currently has no role gating at all (Admin can load/submit the whole page today). This new card must only render for `Role.Owner` — same `user.roles.includes(Role.Owner)` check pattern as `TimeOffsPage.tsx`'s `isManagement`. The existing Payment Policy form's access is unchanged (still open to Owner+Admin, matching current behavior).

## Out of scope

- Enable/disable toggle (`TenantPaymentCredential.Disable()` stays unused).
- "Test connection" validation call against PayMongo's API.
- Partial credential updates (change only public key, or only secret key) — not supported by the domain method.

## Testing

- Backend: manual verification via Swagger/REST client against a local run — matches the project's current test coverage level (no handler/controller test suite exists to extend, per the prior spec's same note).
  - Verify `GET` returns `isConfigured: false` for a tenant with no credential.
  - Verify `PUT` with a valid `sk_test_...`/`pk_test_...` pair succeeds (204), then `GET` reflects `isConfigured: true`, correct `mode`, masked key format.
  - Verify `PUT` with a malformed key (wrong prefix) returns 400 with field-level errors.
  - Verify an Admin-role JWT gets 403 on both `GET` and `PUT`.
- Frontend: manual — log in as Owner, confirm the card loads correctly in both configured/unconfigured states, submit valid/invalid values, confirm toast + persisted state after refetch. Log in as Admin, confirm the card doesn't render at all.
