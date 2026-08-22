# Payment & Booking Security Hardening

## Context

An architecture/compliance audit of the PayMongo "Direct Connect" payment flow and the booking
engine surfaced four gaps, all confirmed against the current codebase (not hypothetical):

1. `TenantPaymentCredential.SecretKey`/`WebhookSecret` are stored **plaintext**
   ([TenantPaymentCredentialConfiguration.cs](../../../src/backend-api/ApexBooking.Core.Persistence/Mappings/TenantPaymentCredentialConfiguration.cs)) — no encryption anywhere in the codebase today.
2. The staff/date collision check only looks at `BookingStatus.Scheduled`
   ([Tenant.cs:434-439](../../../src/backend-api/ApexBooking.Core.Domain/Entities/Tenant.cs#L434-L439)), so two customers can both land in `PendingPayment` for the same slot — `InitiateBookingHandler.cs` has no locking of any kind. There's also no mechanism to reclaim a slot abandoned mid-checkout (`BookingStatus` has no `Expired` member).
3. `ProcessPaymentWebhookCommandHandler.cs` already derives the tenant from the booking DB rather than trusting the payload (good), but has no idempotency ledger and no signature-age check. `PayMongoWebhooksController.cs:73` logs the **raw JSON payload** on any exception — a DPA violation.
4. No documented boundary on what must never be logged, and no ToS language covering PayMongo's role, billing-dispute indemnification, or DPA 2012 data-role splits.

This spec covers hardening the **existing** Direct Connect flow only. It explicitly does **not**
implement PayMongo's Platform product (child-account onboarding, KYC, Account-Id payment routing,
Payment Links migration, Checkout Sessions, Payment Intents, merchant lifecycle webhooks, or
retiring the current per-tenant credential model) — that is a separate migration, gated on
confirming the PayMongo contract and legal terms for that model.

## Decisions (confirmed with user)

- Stale `PendingPayment` cutoff: **30 minutes**.
- No plaintext→ciphertext data migration — this is a dev environment with no real tenant data.
- Data Protection key ring persists in SQL Server via EF Core (`PersistKeysToDbContext`), not local disk or a cloud KMS (no Azure/AWS wired into this codebase today).
- ToS boilerplate uses a `[Company Legal Name]` placeholder, not a real entity name.
- **Process constraints for this effort**: EF migrations are written as code but **not applied** (`dotnet ef database update` is not run) against any database. Nothing in this effort is `git commit`ed. No `git reset`/`checkout`/`stash`/discard of any file, and all pre-existing uncommitted work in the tree is left untouched.
- Module 2's lock and the booking write must share one transaction and one `DbContext` instance (see below) — a separate connection/transaction for the lock would make it ineffective.
- Module 3's booking-status update and idempotency-ledger insert must commit atomically — no window where the event is marked processed before the payment state change has actually succeeded.

## Module 1 — Credential envelope encryption

**Ports**: `ISecretProtector` (`Core.Domain.Services`) with `Protect(string)`/`Unprotect(string)`.
`DataProtectionSecretProtector` (`Infrastructure`) wraps `IDataProtectionProvider.CreateProtector("ApexBooking.TenantPaymentCredentials.v1")` — a distinct purpose string so this key ring is cryptographically isolated from any future Data Protection use elsewhere in the app.

**Key ring persistence**: `AddDataProtection().PersistKeysToDbContext<ApexBookingDbContext>().SetApplicationName("ApexBooking")`, registered in `InfrastructureDependencies.cs`. This requires:
- The `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` package.
- `ApexBookingDbContext` to implement `IDataProtectionKeyContext` (adds `DbSet<DataProtectionKey> DataProtectionKeys`).
- A new migration adding the `DataProtectionKeys` table (generated, not applied — see constraints above).

**The EF value-converter wrinkle**: `TenantPaymentCredentialConfiguration` is currently discovered via `builder.ApplyConfigurationsFromAssembly(...)` in `OnModelCreating`, which instantiates configuration classes with a parameterless constructor — it cannot constructor-inject `ISecretProtector`. Plan: inject `IDataProtectionProvider` into `ApexBookingDbContext` itself (it already takes constructor-injected services, e.g. `ITenantEntity`), build the protector once in `OnModelCreating`, and apply `TenantPaymentCredentialConfiguration` explicitly with that instance — excluding just this one type from the assembly scan via the `ApplyConfigurationsFromAssembly(Assembly, Func<Type,bool>)` predicate overload so it isn't double-registered:

```csharp
builder.ApplyConfigurationsFromAssembly(typeof(ApexBookingDbContext).Assembly,
    t => t != typeof(TenantPaymentCredentialConfiguration));
builder.ApplyConfiguration(new TenantPaymentCredentialConfiguration(_secretProtector));
```

**Mapping**: `SecretKey` and `WebhookSecret` get a `ValueConverter<string, string>` (`v => _secretProtector.Protect(v)`, `v => _secretProtector.Unprotect(v)`) — `WebhookSecret`'s converter must handle `null` (it's optional pre-webhook-registration). `PublicKey` stays plaintext (publishable key, safe to expose). Max length raised from `500` to `1000` on the two encrypted columns — Data Protection ciphertext (AES-256-CBC + HMAC, base64-encoded, plus key-id header) runs meaningfully larger than the ~70-char raw PayMongo keys; `1000` gives comfortable headroom without being unbounded.

## Module 2 — Race condition & expiration

**Locking**: a new `IUnitOfWork.AcquireBookingLockAsync(string resourceKey, TimeSpan timeout, CancellationToken)` returning an `IBookingLockScope : IAsyncDisposable` (with `Task CommitAsync(CancellationToken)`). Implemented in `UnitOfWork` against the *same* `_context` it already owns — critically, this is not a standalone service with its own `DbContext`, because the transaction the lock lives in must be the same transaction the booking write commits through:

```
_context.Database.BeginTransactionAsync()
  → EXEC sp_getapplock @Resource=<key>, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=<ms>
      (non-zero/negative result → throw a friendly BusinessRuleBrokenException, caller retries)
  → [handler runs its normal EF reads/writes through _unitOfWork.TenantRepository, same _context]
  → _unitOfWork.CompleteAsync()   (SaveChangesAsync automatically joins the ambient transaction — EF Core
                                    does not open a second one when Database.CurrentTransaction is set)
  → lockScope.CommitAsync()        (commits the transaction; sp_getapplock's Transaction-owned lock
                                     releases automatically on commit)
```

`await using` on the scope means an exception anywhere in the handler rolls the transaction back (and releases the lock) without an explicit `Commit()` call.

**Resource key**: `booking:{tenantId}:{staffId}:{date:yyyyMMdd}` — matches the exact dimension `Tenant.cs`'s collision check already uses (`StaffId` + `ScheduledDate`), per the audit's explicit requirement. `InitiateBookingHandler` can build this from `command.StaffId`/`command.ScheduledDate` and the ambient tenant id before it even loads the tenant, so the lock is acquired first, cheaply.

**Collision check fix**: [Tenant.cs:437](../../../src/backend-api/ApexBooking.Core.Domain/Entities/Tenant.cs#L437) — add `BookingStatus.PendingPayment` alongside `Scheduled` in the `collidesWithExistingBooking` predicate.

**Expiration**: `BookingStatus.Expired = 6`. `Booking.ExpirePendingPayment()` — guards `Status == PendingPayment` (same guard style as the rest of `Booking.cs`), sets `Status = Expired`, `UpdatedAt`, raises `BookingExpiredDomainEvent : IReliableDomainEvent` (shape mirrors `BookingCancellationNoticeDomainEvent`: `TenantId, BookingId, BookingReference, CustomerId, ExpiredAt`) — reliable because it drives a customer-facing notice email.

**Concurrency token**: `Booking.RowVersion` (`byte[]`, EF `.IsRowVersion()` → SQL Server `rowversion` column, DB-generated). This makes `DbUpdateConcurrencyException` automatic on a conflicting `SaveChanges` — no manual version-compare code needed. This is what lets a live webhook (`ConfirmPayment`) and the sweep job (`ExpirePendingPayment`) racing on the same row fail safely instead of silently corrupting state.

**`ExpireStalePendingBookingsJob`**: same shape as `TrialExpiryJob` (per-tenant `try/catch`, one `CompleteAsync()` per tenant so one tenant's failure — including a `DbUpdateConcurrencyException` from a webhook that just won the race — doesn't block the rest of the sweep). Needs a new `ITenantRepository.GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken)` (same "Booking is a Tenant child, escape-hatch query" rationale as `GetByBookingIdAsync`) returning tenants with only their stale bookings loaded. Registered as a new `RecurringJob` in `HangfireServiceExtensions.cs`, proposed cadence **every 5 minutes** (`Cron.MinuteInterval(5)`) — frequent enough to reclaim a 30-minute-stale slot promptly without being wasteful; flag for adjustment if you'd prefer a different interval.

## Module 3 — Webhook idempotency & safe logging

**Event id capture**: `PaymongoContracts.cs`'s `WebhookData` class doesn't currently capture the envelope's own `id` (PayMongo's `evt_...` event id lives at `payload.Data.Id` — distinct from `WebhookResource.Id`, which is the Link's id nested one level deeper). Add `[JsonPropertyName("id")] public string Id { get; set; }` to `WebhookData`. `ProcessPaymentWebhookCommand` gains a `PayMongoEventId` parameter; the controller passes `payload.Data.Id` through.

**Ledger**: `ProcessedPaymentEvent` — a plain persistence record (same pattern as `OutboxMessage`: `Guid Id`, no `ITenantEntity`, no repository) with `PayMongoEventId` (string, **unique index**), `TenantId` (`Guid`, informational only), `BookingId` (`Guid`), `ProcessedAt` (`DateTime`). Exposed via a small `IProcessedPaymentEventStore` port (`Core.Domain.Services`, same naming convention as `IRefundRequestStore`) with `ExistsAsync(string payMongoEventId, ...)` and `Add(ProcessedPaymentEvent)`, implemented in `Persistence/Services` against the same scoped `ApexBookingDbContext` the request's `UnitOfWork` uses — injected directly into the handler alongside `IUnitOfWork` (same pattern as the handler's existing direct `IPayMongoWebhookSignatureVerifier` dependency).

**Atomicity**: because the store's `Add` and `IUnitOfWork.TenantRepository.Update` both mutate the *same* `DbContext` instance (ASP.NET Core scoped DI — one instance per request), a single `_unitOfWork.CompleteAsync()` call commits the booking status change and the ledger row in the same `SaveChangesAsync`/transaction. No new transaction-management code is needed in `UnitOfWork` itself for this module — this falls out of the existing scoping. The unique index is also a DB-level safety net against a same-event double-delivery race slipping past the app-level `ExistsAsync` check (TOCTOU) — a concurrent duplicate insert throws a unique-constraint `DbUpdateException`, which the handler catches and treats as a benign duplicate (log + return), not an error.

**Handler flow** (order adjusted slightly from the literal ask — signature verification unavoidably needs the tenant's webhook secret, so tenant resolution has to happen before the signature+age check, not after):

1. Guard `RemarksToken` format (unchanged) → extract `targetBookingId`.
2. Resolve tenant via `GetByBookingIdAsync` (unchanged).
3. Verify signature against `tenant.PaymentCredential.WebhookSecret` (unchanged) — extend `IPayMongoWebhookSignatureVerifier` with `TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp)` (reuses the existing header-parsing logic) so the handler can additionally reject anything where `DateTimeOffset.UtcNow - timestamp > TimeSpan.FromMinutes(5)`.
4. **Ledger check**: `await _processedPaymentEventStore.ExistsAsync(command.PayMongoEventId, ct)` — if true, `_logger.LogInformation(...)` and return (clean no-op, still 200 to PayMongo). Handler needs `ILogger<ProcessPaymentWebhookCommandHandler>` added to its constructor.
5. Resolve the `Booking` child entity, call `booking.ConfirmPayment(...)` (unchanged).
6. `_processedPaymentEventStore.Add(ProcessedPaymentEvent.Create(command.PayMongoEventId, tenant.TenantId.Value, booking.BookingId.Value))`.
7. `_unitOfWork.TenantRepository.Update(tenant); await _unitOfWork.CompleteAsync(ct);` — one commit for both.

**Controller logging fix**: `PayMongoWebhooksController.cs:73`'s catch block stops interpolating `jsonText` into the log message. Logs `SHA256(jsonText)` (hex) plus a fresh correlation `Guid` instead — enough to correlate a support ticket or PayMongo's own delivery logs against ours without ever persisting cardholder-adjacent payload content. The `BadRequest` response behavior for genuine failures is unchanged (PayMongo should keep retrying those) — only what gets logged changes.

## Module 4 — Logging boundary & ToS boilerplate (docs only, no code)

Delivered as a short markdown note plus boilerplate text (not part of this spec file) covering:
- What must never be logged (`Authorization` header, raw webhook bodies) and why the fix is "don't add a body/header-capturing logger" rather than adding one with a redaction allowlist — this app has no `UseHttpLogging()` or similar middleware today, and the one place a raw payload *was* being logged is fixed directly in Module 3.
- ToS sections: No Financial Custody / Technical Intermediary Status; Indemnification of Billing Disputes/Refunds; DPA 2012 Data Role Splits (PIC vs PIP) — with `[Company Legal Name]` placeholders throughout.

## Out of scope

Everything under the future "PayMongo Platform migration": child-account onboarding, KYC,
Account-Id payment routing, Payment Links migration, Checkout Sessions, Payment Intents,
merchant lifecycle webhooks, and retiring the current per-tenant credential model.

## Testing

- **Domain unit tests** (`ApexBooking.Core.Domain.UnitTests`, xUnit — matches existing `BookingPaymentCaptureTests.cs` conventions): `Booking.ExpirePendingPayment()` happy path + guard-throws when not `PendingPayment`, and that `BookingExpiredDomainEvent` is raised with the right payload.
- **Manual verification** for everything else, matching this codebase's established testing level (no handler/controller test suite exists to extend, per prior specs' same note):
  - Encryption round-trip: configure credentials via the existing `PUT /api/Tenant/payment-gateway`, confirm the DB row is ciphertext, confirm `InitiateBookingHandler` still reads a working `SecretKey` back.
  - Concurrent booking: fire two parallel `InitiateBooking` requests for the same staff/slot, confirm exactly one succeeds.
  - Webhook replay: send the same PayMongo event id twice, confirm the second is a no-op 200 with no duplicate `PaymentCapturedDomainEvent`/ledger row.
  - Signature-age rejection: replay a webhook with an old `t=` timestamp, confirm it's rejected.
- No `dotnet ef database update` is run as part of this effort; migrations are code-only until you choose to apply them.
