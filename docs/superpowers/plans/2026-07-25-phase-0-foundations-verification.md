# Phase 0 — Foundations Verification Record

Date: 2026-07-25. Companion to `2026-07-25-phase-0-foundations.md`.

## Gate results

| Gate | Result |
|---|---|
| Solution builds (`dotnet build ApexBooking.sln`) | ✅ 0 errors (only pre-existing `NU1903` NuGet vulnerability warnings) |
| Domain-event pipeline dispatches through the interceptor | ✅ `DomainEventPipelineTests` (2 tests) pass — throwaway aggregate raising a test event dispatches at `SaveChanges` and is cleared |
| Pipeline registered in DI | ✅ `DomainEventRegistrationTests` (2 tests) pass — `IDomainEventDispatcher` in Application, `DispatchDomainEventsInterceptor` in Persistence |
| Dependency-direction grep (new foundations) | ✅ clean — see below |

## Boundary grep

- **Domain** — no `using Microsoft.EntityFrameworkCore` / `using MediatR` / `using AutoMapper`. `ApexBooking.Core.Domain.csproj` references only `ApexBooking.SharedKernel` + `ApexBooking.GenericRepository.Abstractions`. Clean.
- **Application** — no `using Microsoft.EntityFrameworkCore` / `using ApexBooking.Core.Persistence`. Clean.

## Deferred — NOT fixed in Phase 0 (owned by later phases)

- **ASP.NET Identity leaks into Domain (ADR-055 / Phase 2 — Identity & Access):**
  - `ApexBooking.Core.Domain/Entities/User.cs` — `public class User : IdentityUser<Guid>, IAggregateRoot, ITenantEntity`
  - `ApexBooking.Core.Domain/Entities/SuperAdmin.cs` — `public class SuperAdmin : IdentityUser<Guid>, IAggregateRoot`
  - `ApexBooking.Core.Domain/Repositories/IUserRepository.cs` — `using Microsoft.AspNetCore.Identity`

  Phase 2 relocates `ApplicationUser` / the ASP.NET Identity store to Persistence per ADR-055. Left as-is here.

- **Strongly-typed IDs (`ValueObjectTenantIdentifier`)** — malformed but functional; ~55-file blast radius, mostly legacy entities slated for rewrite. Cleaned up per-aggregate as each aggregate is rebuilt (Phase 1+). Not touched.

## Confirmed already-satisfied (no work needed)

- **Rule 9** — `GlobalExceptionHandler` maps `UnauthorizedException`/`NotFoundException`/`BusinessRuleBrokenException`/`ValidationException`/`DbUpdateConcurrencyException` + fallback 500.
- **Rule 10** — `Program.cs` wires controllers, exception handler, CORS, rate limiting, auth, `MapControllers()`.
- **CORS** — `ApplicationCorsPolicy` (`AddApplicationCors`). **Per-IP rate limiting** — sliding-window partitioned by remote IP (`AddApplicationRateLimiting`).
- **Infra-service interface placement** — `ICookieService`, `ITokenService` already in Domain. `IEmailService` / `ISmsService` / `ISmsQuotaService` / `IPayMongoService` / `IPlanPolicy` / `IReportingQueries` / `IBillingQueries` do not exist yet and are created in their own phases (Notification / Billing / Reporting).
