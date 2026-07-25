# Phase 0 — Foundations & Boundary Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Shared Kernel consistent, wire the domain-event pipeline so a raised event actually dispatches through the `SaveChanges` interceptor, and remove the orphaned `ApexBooking.Identity` project — so every later context phase builds on a correct foundation.

**Architecture:** Onion / DDD, CQRS via MediatR, EF Core 10, ASP.NET Core controllers. Domain events are raised only by aggregate roots and dispatched inside `SaveChangesAsync` by `DispatchDomainEventsInterceptor` (Persistence) → `IDomainEventDispatcher` (Domain interface) → `DomainEventDispatcher` (Application impl, wraps each `IDomainEvent` in `DomainEventNotification<T>` and publishes via MediatR `IPublisher`).

**Tech Stack:** .NET 10, EF Core 10.0.9, MediatR 14.1.0, xUnit 2.9.3.

## Global Constraints

Every task implicitly includes these (verbatim from `12-refactoring-roadmap.md` and `02` §11):

- **Dependency direction points inward.** Domain depends only on `ApexBooking.SharedKernel` + `ApexBooking.GenericRepository.Abstractions`. No EF Core, MediatR, or AutoMapper references in Domain. Application may reference MediatR/AutoMapper/FluentValidation but not Persistence/Infrastructure/WebApi/EF Core. Persistence is the only home for `ApexBookingDbContext` + the interceptor. WebApi is the composition root.
- **Shared Kernel is a leaf** — references nothing; holds marker interfaces, `BusinessRuleBrokenException`, `QueryResult`/`QueryObjectParams`.
- **Only aggregate roots (`IAggregateRoot`) implement `IHasDomainEvents`.** Child entities never raise their own events (`03` Rule 13).
- **The event catalog (`02` §7) is authoritative** — no handler for an unlisted event; Phase 0 raises no real business events (it uses a throwaway test aggregate only).
- **`GlobalExceptionHandler` maps exceptions to status codes** (`03` Rule 9); status codes are never set in controllers/handlers.

---

## Current-state findings (read before starting — the roadmap predates these)

The solution **builds clean today** (`dotnet build ApexBooking.sln` → 0 errors, only NuGet `NU1903` vulnerability warnings). Much of what the roadmap lists as Phase 0 work already exists:

- `GlobalExceptionHandler` — **complete** at [ApexBooking.WebApi/Infrastructure/GlobalExceptionHandler.cs](../../../ApexBooking.WebApi/Infrastructure/GlobalExceptionHandler.cs) (handles `UnauthorizedException`, `NotFoundException`, `BusinessRuleBrokenException`, `ValidationException`, `DbUpdateConcurrencyException`, fallback 500).
- `Program.cs` — **already** wires controllers, `AddExceptionHandler<GlobalExceptionHandler>`, CORS (`AddApplicationCors`), per-IP rate limiting (`AddApplicationRateLimiting`), auth, `MapControllers()`. Satisfies `03` Rule 10.
- CORS policy `ApplicationCorsPolicy` and per-IP sliding-window rate limiting — **implemented** in [CorsConfigurationExtensions.cs](../../../ApexBooking.WebApi/Configuration/CorsConfigurationExtensions.cs) and [RateLimitingExtensions.cs](../../../ApexBooking.WebApi/Configuration/RateLimitingExtensions.cs).
- `DispatchDomainEventsInterceptor` — **written** ([ApexBooking.Core.Persistence/Interceptors/DispatchDomainEventsInterceptor.cs](../../../ApexBooking.Core.Persistence/Interceptors/DispatchDomainEventsInterceptor.cs)) but **not registered / not attached** to the DbContext.
- `DomainEventDispatcher` + `DomainEventNotification<T>` — **already written correctly** in Application ([Common/DomainEvent/](../../../ApexBooking.Core.Application/Common/DomainEvent/)) matching `03` Rule 13 verbatim, but **not registered** in DI.
- `ICookieService`, `ITokenService` — already live in Domain (`Services/Cookie`, `Services/TokenService`). The other infra-service interfaces named in the roadmap (`IEmailService`, `ISmsService`, `IPayMongoService`, `IPlanPolicy`, `IReportingQueries`, `IBillingQueries`, `ISmsQuotaService`) **do not exist yet** — they are created in their own later phases (Notification/Billing/Reporting), not here.
- `ApexBooking.Identity` — an **orphan not even listed in `ApexBooking.sln`**. It contains only `Class1.cs` and an empty `ApplicationUser` class. Retiring it = deleting the folder.

**So Phase 0 reduces to three real changes** (namespace unification, pipeline wiring, delete the orphan) plus a verification gate.

## Non-goals (explicit scope boundaries — do NOT do these here)

- **Do not replace `ValueObjectTenantIdentifier` / redesign strongly-typed IDs.** Per the locked decision (minimal/YAGNI, no base `Entity`, no ID base class), the malformed-but-functional `ValueObjectTenantIdentifier` is left alone: it has a ~55-file blast radius (nearly every entity/mapping/repo, most of it legacy slated for rewrite). Each aggregate's ID is cleaned up when that aggregate is rebuilt (Phase 1+).
- **Do not build a base `Entity<T>` or strongly-typed-ID base class.** Locked decision: aggregates and IDs stay as plain classes/records, matching the `03` examples.
- **Do not rewrite the `Tenant` aggregate** or add real domain events to it — that is Phase 1. Phase 0 touches `Tenant.cs` only to fix a `using`.
- **Do not fix `User : IdentityUser<Guid>` in Domain.** ASP.NET Identity in Domain is a known ADR-055 violation owned by Phase 2 (Identity & Access). Phase 0 records it as a deferred finding, nothing more.
- **Do not modify `Program.cs`, `GlobalExceptionHandler`, CORS, or rate limiting** beyond verifying them — they already satisfy Rules 9/10.

## File Structure

**Modified:**
- `Framework.Core/SharedKernel/Models/IDomainEvent.cs` — namespace fix
- `Framework.Core/SharedKernel/Models/IHasDomainEvents.cs` — namespace fix
- `Framework.Core/SharedKernel/Models/IDomainEventDispatcher.cs` — namespace fix
- `ApexBooking.Core.Application/Common/DomainEvent/DomainEventDispatcher.cs` — `using` fix
- `ApexBooking.Core.Application/Common/DomainEvent/DomainEventNotification.cs` — `using` fix
- `ApexBooking.Core.Domain/Entities/Tenant.cs` — remove redundant `using`
- `ApexBooking.Core.Persistence/Interceptors/DispatchDomainEventsInterceptor.cs` — `using` fix
- `ApexBooking.Core.Application/Dependency/ApplicationServices.cs` — register dispatcher
- `ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs` — register + attach interceptor
- `ApexBooking.Core.Domain.UnitTests/ApexBooking.Core.Domain.UnitTests.csproj` — add EF Core InMemory test package

**Created:**
- `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventPipelineTests.cs` — pipeline proof (the Phase 0 gate)
- `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventRegistrationTests.cs` — DI-registration RED→GREEN

**Deleted:**
- `ApexBooking.Identity/` — the whole orphaned project folder

---

### Task 1: Unify the Shared Kernel event-interface namespace

Three event interfaces are declared in `namespace SharedKernel.Models`; the rest of the Shared Kernel uses `namespace ApexBooking.SharedKernel.Models`. That split forces every consumer to import both. Unify to `ApexBooking.SharedKernel.Models`.

**Files:**
- Modify: `Framework.Core/SharedKernel/Models/IDomainEvent.cs`, `IHasDomainEvents.cs`, `IDomainEventDispatcher.cs`
- Modify: `ApexBooking.Core.Application/Common/DomainEvent/DomainEventDispatcher.cs:6`, `DomainEventNotification.cs:6`
- Modify: `ApexBooking.Core.Domain/Entities/Tenant.cs:6`
- Modify: `ApexBooking.Core.Persistence/Interceptors/DispatchDomainEventsInterceptor.cs:7`

**Interfaces:**
- Consumes: nothing.
- Produces: `IDomainEvent`, `IHasDomainEvents`, `IDomainEventDispatcher` all in namespace `ApexBooking.SharedKernel.Models` (joining `IAggregateRoot`, `QueryResult<T>`, `QueryObjectParams`).

- [ ] **Step 1: Change the namespace in the three interface files**

In each of `IDomainEvent.cs`, `IHasDomainEvents.cs`, `IDomainEventDispatcher.cs`, change the declaration line:

```csharp
namespace SharedKernel.Models
```
to:
```csharp
namespace ApexBooking.SharedKernel.Models
```
(Leave the interface bodies untouched.)

- [ ] **Step 2: Fix the four consumers' `using` directives**

In `DomainEventDispatcher.cs`, `DomainEventNotification.cs`, and `DispatchDomainEventsInterceptor.cs`, replace:
```csharp
using SharedKernel.Models;
```
with:
```csharp
using ApexBooking.SharedKernel.Models;
```

In `Tenant.cs`, **delete** line 6 (`using SharedKernel.Models;`) entirely — line 5 already imports `using ApexBooking.SharedKernel.Models;`, which now covers `IDomainEvent`/`IHasDomainEvents` too.

- [ ] **Step 3: Verify no bare `SharedKernel.Models` references remain**

Run:
```bash
grep -rn "\bSharedKernel\.Models" --include="*.cs" . --exclude-dir=obj --exclude-dir=bin | grep -v "ApexBooking.SharedKernel.Models"
```
Expected: **no output** (every occurrence is now the prefixed form).

- [ ] **Step 4: Build the solution**

Run: `dotnet build ApexBooking.sln --nologo -v q`
Expected: `Build succeeded`, `0 Error(s)` (NU1903 warnings are pre-existing and unrelated).

- [ ] **Step 5: Commit**

```bash
git add Framework.Core/SharedKernel/Models ApexBooking.Core.Application/Common/DomainEvent ApexBooking.Core.Domain/Entities/Tenant.cs ApexBooking.Core.Persistence/Interceptors/DispatchDomainEventsInterceptor.cs
git commit -m "refactor: unify SharedKernel event interfaces into ApexBooking.SharedKernel.Models namespace"
```

---

### Task 2: Wire and prove the domain-event pipeline

The dispatcher, notification wrapper, and interceptor all exist but nothing registers them, so a raised event is never dispatched or cleared. Register both and attach the interceptor to `ApexBookingDbContext`, proven by tests first.

**Files:**
- Modify: `ApexBooking.Core.Domain.UnitTests/ApexBooking.Core.Domain.UnitTests.csproj`
- Create: `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventPipelineTests.cs`
- Create: `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventRegistrationTests.cs`
- Modify: `ApexBooking.Core.Application/Dependency/ApplicationServices.cs`
- Modify: `ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`

**Interfaces:**
- Consumes: `IDomainEvent`, `IHasDomainEvents`, `IDomainEventDispatcher` (namespace `ApexBooking.SharedKernel.Models`, after Task 1); `DomainEventDispatcher`, `DomainEventNotification<T>` (namespace `ApexBooking.Core.Application.Common.DomainEvent`); `DispatchDomainEventsInterceptor` (namespace `ApexBooking.Core.Persistence.Interceptors`).
- Produces: DI registrations — `AddApplicationServices` registers `IDomainEventDispatcher` → `DomainEventDispatcher`; `AddPersistenceServices` registers `DispatchDomainEventsInterceptor` and attaches it to the `ApexBookingDbContext` options.

- [ ] **Step 1: Add the EF Core InMemory test package**

In `ApexBooking.Core.Domain.UnitTests.csproj`, add inside the existing package `<ItemGroup>` (the one with xunit):
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.9"/>
```
Run: `dotnet restore ApexBooking.Core.Domain.UnitTests/ApexBooking.Core.Domain.UnitTests.csproj`
Expected: restore succeeds.

- [ ] **Step 2: Write the failing pipeline test (the gate)**

Create `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventPipelineTests.cs`:

```csharp
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Persistence.Interceptors;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Domain.UnitTests.DomainEvents;

public class DomainEventPipelineTests
{
    // --- Throwaway aggregate + event (Phase 0 uses no real business events) ---
    private sealed record ThingHappened(Guid Id) : IDomainEvent;

    private sealed class Thing : IAggregateRoot, IHasDomainEvents
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
        public void ClearDomainEvents() => _events.Clear();

        public void DoSomething() => _events.Add(new ThingHappened(Id));
    }

    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions options) : base(options) { }
        public DbSet<Thing> Things => Set<Thing>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Thing>().HasKey(t => t.Id);
            b.Entity<Thing>().Ignore(t => t.DomainEvents);
        }
    }

    // Spy standing in for the real IDomainEventDispatcher, to prove the
    // interceptor invokes it at SaveChanges.
    private sealed class SpyDispatcher : IDomainEventDispatcher
    {
        public List<IHasDomainEvents> Received { get; } = new();
        public Task DispatchAndClearAsync(IEnumerable<IHasDomainEvents> entitiesWithEvents)
        {
            var list = entitiesWithEvents.ToList();
            Received.AddRange(list);
            foreach (var e in list) e.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    // Spy standing in for MediatR IPublisher, to prove the real dispatcher
    // wraps + publishes + clears.
    private sealed class SpyPublisher : IPublisher
    {
        public List<object> Published { get; } = new();
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification!);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Interceptor_dispatches_domain_events_at_SaveChanges()
    {
        var spy = new SpyDispatcher();
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase("evt-" + Guid.NewGuid())
            .AddInterceptors(new DispatchDomainEventsInterceptor(spy))
            .Options;

        await using var ctx = new TestContext(options);
        var thing = new Thing();
        thing.DoSomething();
        ctx.Things.Add(thing);

        await ctx.SaveChangesAsync();

        Assert.Single(spy.Received);
        Assert.Empty(thing.DomainEvents); // interceptor path cleared the events
    }

    [Fact]
    public async Task Dispatcher_wraps_event_in_notification_and_clears()
    {
        var publisher = new SpyPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var thing = new Thing();
        thing.DoSomething();

        await dispatcher.DispatchAndClearAsync(new IHasDomainEvents[] { thing });

        Assert.Single(publisher.Published);
        Assert.IsType<DomainEventNotification<ThingHappened>>(publisher.Published[0]);
        Assert.Empty(thing.DomainEvents);
    }
}
```

- [ ] **Step 3: Run the pipeline test — expect PASS**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "FullyQualifiedName~DomainEventPipelineTests" --nologo`
Expected: **both tests PASS** — the interceptor and dispatcher code is already correct; these tests characterize and lock it in. (If either fails, stop and debug the interceptor/dispatcher before proceeding — that is a real defect, not a wiring gap.)

- [ ] **Step 4: Write the failing DI-registration test**

Create `ApexBooking.Core.Domain.UnitTests/DomainEvents/DomainEventRegistrationTests.cs`:

```csharp
using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Persistence.Dependencies;
using ApexBooking.Core.Persistence.Interceptors;
using ApexBooking.SharedKernel.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Core.Domain.UnitTests.DomainEvents;

public class DomainEventRegistrationTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddApplicationServices_registers_the_domain_event_dispatcher()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices(Config(new()));

        Assert.Contains(services, d => d.ServiceType == typeof(IDomainEventDispatcher));
    }

    [Fact]
    public void AddPersistenceServices_registers_the_dispatch_interceptor()
    {
        var services = new ServiceCollection();
        var config = Config(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=True;"
        });

        services.AddPersistenceServices(config);

        Assert.Contains(services, d => d.ServiceType == typeof(DispatchDomainEventsInterceptor));
    }
}
```

- [ ] **Step 5: Run the registration test — expect FAIL**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "FullyQualifiedName~DomainEventRegistrationTests" --nologo`
Expected: **both tests FAIL** — `IDomainEventDispatcher` and `DispatchDomainEventsInterceptor` are not registered yet.

- [ ] **Step 6: Register the dispatcher in Application**

In `ApexBooking.Core.Application/Dependency/ApplicationServices.cs`, add the `using` at the top:
```csharp
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.SharedKernel.Models;
```
and inside `AddApplicationServices`, after the `AddScoped(typeof(IPipelineBehavior<,>), ...)` line, add:
```csharp
// Domain-event dispatch (wraps IDomainEvent -> MediatR notification)
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

- [ ] **Step 7: Register + attach the interceptor in Persistence**

In `ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs`, add the `using`:
```csharp
using ApexBooking.Core.Persistence.Interceptors;
```
Replace the current `AddDbContext` registration:
```csharp
services.AddDbContext<ApexBookingDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```
with:
```csharp
services.AddScoped<DispatchDomainEventsInterceptor>();

services.AddDbContext<ApexBookingDbContext>((sp, options) =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));
```
(The `DomainEventDispatcher` the interceptor depends on is resolved from DI at DbContext construction time; it is registered by `AddApplicationServices`, which `Program.cs` calls before `AddPersistenceServices`.)

- [ ] **Step 8: Run the registration test — expect PASS**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "FullyQualifiedName~DomainEventRegistrationTests" --nologo`
Expected: **both tests PASS**.

- [ ] **Step 9: Full build + full test run**

Run: `dotnet build ApexBooking.sln --nologo -v q` then `dotnet test ApexBooking.Core.Domain.UnitTests --nologo`
Expected: build `0 Error(s)`; all tests pass.

- [ ] **Step 10: Commit**

```bash
git add ApexBooking.Core.Application/Dependency/ApplicationServices.cs ApexBooking.Core.Persistence/Dependencies/PersistenceDependency.cs ApexBooking.Core.Domain.UnitTests
git commit -m "feat: wire domain-event pipeline (register dispatcher + attach SaveChanges interceptor) with tests"
```

---

### Task 3: Retire the orphaned `ApexBooking.Identity` project

The project is not referenced by any `.csproj` and is not listed in `ApexBooking.sln`; it holds only `Class1.cs` and an empty `ApplicationUser`. Per ADR-055 the standalone Identity project is retired (its real responsibilities move to Persistence in Phase 2). Nothing of value exists to relocate, so this is a straight deletion.

**Files:**
- Delete: `ApexBooking.Identity/` (entire folder)

- [ ] **Step 1: Confirm nothing references the project**

Run:
```bash
grep -rl "ApexBooking.Identity" --include="*.csproj" . ; grep -c "ApexBooking.Identity" ApexBooking.sln
```
Expected: no `.csproj` matches; `ApexBooking.sln` count is `0`. (If either shows a reference, stop — the assumption is wrong and the deletion needs a `dotnet sln remove` / reference-removal step first.)

- [ ] **Step 2: Delete the project folder**

Run:
```bash
git rm -r ApexBooking.Identity
```
(If the folder is untracked rather than tracked, use `rm -rf ApexBooking.Identity` instead.)

- [ ] **Step 3: Build**

Run: `dotnet build ApexBooking.sln --nologo -v q`
Expected: `Build succeeded`, `0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: retire orphaned ApexBooking.Identity project (ADR-055)"
```

---

### Task 4: Boundary & foundations verification gate

Confirm the Phase 0 gate (`solution builds; dependency-direction grep is clean; a throwaway aggregate raising a test event dispatches through the interceptor`) and record known-deferred boundary violations so later phases inherit an honest baseline.

**Files:**
- Create: `docs/superpowers/plans/2026-07-25-phase-0-foundations-verification.md` (findings record)

- [ ] **Step 1: Confirm the pipeline gate**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter "FullyQualifiedName~DomainEventPipelineTests" --nologo`
Expected: PASS — this is the "throwaway aggregate raising a test event dispatches through the interceptor" gate.

- [ ] **Step 2: Grep Domain + Application for forbidden framework references**

Run each and record results:
```bash
# Domain must not use EF Core, MediatR, or AutoMapper
grep -rn "using Microsoft.EntityFrameworkCore\|using MediatR\|using AutoMapper" --include="*.cs" ApexBooking.Core.Domain
# Application must not use EF Core or Persistence internals
grep -rn "using Microsoft.EntityFrameworkCore\|using ApexBooking.Core.Persistence" --include="*.cs" ApexBooking.Core.Application
```
Expected for the domain-event foundations touched in this phase: **no new violations**. Note `ApexBooking.Core.Domain.csproj` references only `SharedKernel` + `GenericRepository.Abstractions` (verified clean at the project level).

- [ ] **Step 3: Record the known-deferred violation**

Write `docs/superpowers/plans/2026-07-25-phase-0-foundations-verification.md` capturing:
- **Deferred (Phase 2 / ADR-055):** `ApexBooking.Core.Domain/Entities/User.cs` declares `public class User : IdentityUser<Guid>` — ASP.NET Identity types leak into Domain. This is owned by Phase 2 (Identity & Access) which relocates `ApplicationUser`/Identity store to Persistence. **Not fixed in Phase 0.**
- **Confirmed placement:** `ICookieService`, `ITokenService` already live in Domain (`Services/Cookie`, `Services/TokenService`). Remaining infra-service interfaces (`IEmailService`, `ISmsService`, `ISmsQuotaService`, `IPayMongoService`, `IPlanPolicy`, `IReportingQueries`, `IBillingQueries`) do not exist yet and are created in their own phases.
- **Confirmed already-satisfied (Rules 9/10):** `GlobalExceptionHandler`, `Program.cs` controller wiring, `ApplicationCorsPolicy`, per-IP rate limiting.

- [ ] **Step 4: Final gate build**

Run: `dotnet build ApexBooking.sln --nologo -v q`
Expected: `Build succeeded`, `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/plans/2026-07-25-phase-0-foundations-verification.md
git commit -m "docs: record Phase 0 boundary verification + deferred ADR-055 finding"
```

---

## Self-Review

**Spec coverage (roadmap Phase 0 bullets):**
- "Verify/complete Shared Kernel" → Task 1 (namespace unification). `IAggregateRoot`, event interfaces, `BusinessRuleBrokenException`, `QueryResult`/`QueryObjectParams` already exist and are correct; base `Entity`/ID base intentionally skipped (locked YAGNI decision, see Non-goals).
- "Wire the domain-event pipeline" → Task 2.
- "Confirm infra-service interfaces live in Domain" → Task 4 Step 3 (confirms the two that exist; rest deferred to their phases).
- "Retire `ApexBooking.Identity`" → Task 3.
- "Establish `Program.cs` wiring, `GlobalExceptionHandler`, CORS, rate-limiting" → already present; Task 4 Step 3 records verification.
- Gate ("builds; grep clean; throwaway aggregate dispatches through interceptor") → Task 4 Steps 1–4.

**Type/name consistency:** `IDomainEventDispatcher.DispatchAndClearAsync(IEnumerable<IHasDomainEvents>)` is used identically in the interceptor, the real dispatcher, and both test spies. `DomainEventNotification<TDomainEvent>` name matches the existing Application record. `DispatchDomainEventsInterceptor` constructor takes `IDomainEventDispatcher` — matched by the InMemory test (`new DispatchDomainEventsInterceptor(spy)`) and the DI attachment (`sp.GetRequiredService<DispatchDomainEventsInterceptor>()`).

**Placeholder scan:** none — every step has exact file paths, exact edits, exact commands, and full test code.
