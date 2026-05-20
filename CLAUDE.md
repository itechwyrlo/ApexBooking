# ApexBooking Backend Architecture

---

## System Overview

ApexBooking is a multitenant booking platform built using:

- ASP.NET Core
- MediatR CQRS
- Domain-Driven Design
- EF Core
- SQL Server
- Clean Architecture

Architecture layers:

- SharedKernel
- GenericRepository
- Domain
- Persistence
- Application
- Infrastructure
- WebApi

The backend is domain-centric. Application orchestrates use cases. Domain owns business rules. Infrastructure implements abstractions. WebApi handles transport concerns only.

---

## Architectural Principles

Core principles:

- Domain-first design
- Explicit behavior over magic
- Rich domain models over anemic models
- Aggregate integrity over convenience
- Tenant isolation by default
- Exception-driven business rule enforcement
- Thin orchestration layers
- Predictable data flow
- Strict boundary enforcement

Prefer:

- Explicit code
- Deterministic flows
- Simple abstractions
- Maintainable architecture

Reject:

- Hidden framework behavior
- Business logic leakage
- Convenience-driven shortcuts
- Architecture drift

---

## Dependency Rules

Allowed dependency flow only:

- SharedKernel
- GenericRepository
- Domain
- Persistence
- Application
- WebApi

Infrastructure implements abstractions from Domain and Application only.

Forbidden dependencies:

- WebApi referenced from lower layers
- Persistence referenced from Domain
- Infrastructure referenced from Domain
- Controllers referenced outside WebApi

Never bypass architecture boundaries for convenience.

---

## Domain Rules

The Domain layer is the single source of truth for business behavior.

Business rules must exist only inside:

- Aggregate roots
- Domain entities
- Domain methods
- Domain services
- Domain policies

Business logic is forbidden inside:

- Controllers
- Repositories
- Infrastructure services
- EF mappings
- Middleware
- Application handlers

Slot availability computation must remain exclusively inside SlotAvailabilityService. Booking state transitions must remain inside Booking aggregate methods.

Domain must:

- Enforce invariants internally
- Reject invalid state immediately
- Throw exceptions for violations
- Never return response wrappers
- Never depend on UI concerns
- Never contain transport concerns

Domain is the authority of business validity.

---

## Domain Invariant Enforcement Rules

Domain invariants must be structurally enforced. Invalid aggregate state must be impossible to create.

### Aggregate Creation Rules

Aggregate creation must occur through:

- Constructors
- Static factory methods
- Aggregate creation methods

Creation logic must validate invariants immediately.

Forbidden:

- Partially initialized aggregates
- Public mutable construction flows
- Creating invalid entities then fixing later

If creation violates business rules, throw a domain exception immediately.

---

### State Transition Rules

All state mutations must occur through aggregate methods only.

Examples:

- booking.Confirm()
- booking.Cancel()
- booking.MarkPaymentCaptured()

Forbidden:

- Public property mutation
- Direct state assignment
- External mutation bypassing aggregate methods

All transitions must validate current state, business rules, and transition legality. Invalid transitions must throw domain exceptions immediately.

---

### Encapsulation Rules

Entities must protect internal state.

Required:

- Private setters
- Protected setters only when necessary
- Readonly collections where possible

Forbidden:

- Public mutable setters
- Mutable aggregate collections
- Uncontrolled entity mutation

Aggregate children must use IReadOnlyCollection.

---

### Child Entity Ownership Rules

Child entities belong exclusively to their aggregate root.

Child entities must not:

- Have standalone repositories
- Be independently mutated
- Bypass aggregate boundaries

All child modifications must occur through parent aggregate methods.

---

### Value Object Rules

Value objects must be immutable. Use records and readonly semantics.

Value objects must:

- Represent concepts
- Validate themselves internally
- Never expose mutable state

---

### Domain Exception Rules

Domain failures represent business rule violations. Domain must throw exceptions immediately when invalid state is attempted, business rules fail, or illegal transitions occur.

Forbidden:

- Returning validation result objects
- Returning boolean failure flags
- Returning transport response objects

Allowed exception examples:

- BusinessRuleException
- InvalidBookingStateException
- SlotUnavailableException

---

## Aggregate Rules

Only Aggregate Roots:

- Implement IAggregateRoot
- Have repositories

Rules:

- Repositories exist only for aggregate roots
- Never expose mutable collections publicly
- Aggregate consistency boundaries must remain protected
- Child entities must always be controlled by aggregate roots

---

## CQRS Rules

Commands mutate state. Queries never mutate state. Controllers must dispatch requests only through IMediator.

Application handlers orchestrate:

- Repositories
- Domain services
- Unit of work

Application handlers must not contain:

- Heavy business logic
- State transition logic
- Invariant enforcement
- Transport formatting

Application layer coordinates only.

---

## Repository Rules

Repositories exist only for Aggregate Roots. Application handlers access persistence only through IUnitOfWork.

Forbidden:

- Injecting repositories into controllers
- Direct DbContext usage outside Persistence
- Exposing IQueryable outside repositories

Repositories must not contain business logic. Repositories perform persistence operations only.

---

## Persistence Rules

EF Core configuration belongs only inside Persistence/Mappings. Use Fluent API configuration. Avoid EF attributes in domain entities.

All migrations originate from ApexBookingDbContext.

Persistence layer must not contain:

- UI logic
- Transport logic
- Business decisions

---

## Mapping Rules

DTO mapping uses static extension method mappings located in ApexBooking.Core.Application/mapper/.

Pattern: entity.ToDto()

Examples:

- booking.ToDetailDto()
- resource.ToPublicResourceDto()
- settings.ToSettingsDto()

Forbidden:

- AutoMapper
- Reflection-based mappers
- Runtime mapping libraries

Mappings must remain explicit, deterministic, strongly typed, and side-effect free.

Mapping methods must never:

- Access database
- Resolve services
- Mutate entities

Mappings transform already-loaded data only.

---

## Multitenancy Rules

Tenant isolation is mandatory. Every tenant-owned query must filter by TenantId.

Tenant context must come only from:

- TenantMiddleware
- IUserContextService

Never trust tenant identifiers from frontend payloads. Cross-tenant access is forbidden unless explicitly authorized for SuperAdmin flows. Never bypass tenant enforcement.

---

## Identity and Security Rules

Authentication rules:

- JWT authentication only
- Refresh token rotation required
- Hashed token storage only

Forbidden:

- Plaintext tokens
- Leaking auth internals
- Sensitive response exposure

Never hardcode API keys, secrets, connection strings, or webhook credentials. Use environment variables and configuration providers. All webhook payloads must be validated. Public endpoints must be rate limited.

---

## Error Handling Architecture

### Core Error Model

Backend uses exception-driven flow.

Flow:

- Domain throws business exceptions
- Application allows exceptions to bubble
- WebApi translates exceptions to HTTP responses
- Frontend displays user-facing meaning

---

### Domain Error Rules

Domain exceptions represent business failures only.

Examples:

- SLOT_UNAVAILABLE
- INVALID_BOOKING_STATE
- TENANT_ACCESS_DENIED

Domain exceptions must only be thrown from within the Domain layer. Application handlers must never throw domain exceptions directly.

Domain must never:

- Generate UI messages
- Return response wrappers
- Return validation result objects

---

### Application Error Rules

Application layer must not:

- Catch business exceptions
- Translate exceptions into UI messages
- Wrap failures into BaseResponse
- Convert failures into success or failure objects

Forbidden:

- try/catch for business flow
- Response envelopes
- Business error formatting

Application layer orchestrates only.

---

### API Error Translation Rules

Only WebApi layer may:

- Catch exceptions globally
- Map exceptions to HTTP codes
- Produce API error contracts

All failures must return errorCode and message. Message must be generic, safe, and non-sensitive.

Forbidden:

- Raw exception messages
- Stack traces
- Internal system details

---

### Forbidden Error Patterns

Forbidden everywhere:

- BaseResponse
- Result
- Success or Failure wrappers
- Boolean business failure returns

Forbidden in handlers:

- try/catch business logic
- Swallowing exceptions
- Converting exceptions to transport models
- Throwing domain exceptions (e.g. BusinessRuleBrokenException) — these belong exclusively in the Domain layer

---

## API Rules

Controllers are thin orchestration layers only.

Controllers may:

- Validate transport concerns
- Dispatch MediatR requests
- Return DTO responses

Controllers must not:

- Contain business logic
- Contain domain rules
- Perform persistence logic
- Manually orchestrate transactions

Success responses are raw DTOs only. Failure responses go through centralized exception middleware only.

---

## Infrastructure Rules

Infrastructure implements abstractions only.

Infrastructure contains:

- Email transport
- Token generation
- External integrations
- Background processing

Infrastructure must never contain:

- Business rules
- Aggregate logic
- Tenant decisions

Email template generation must not live inside handlers. Reusable templates belong in dedicated template services.

---

## Background Job Rules

Background jobs must:

- Be idempotent
- Be retry-safe
- Avoid controller coupling
- Avoid UI dependencies

Current authoritative background automation: TrialExpiryJob. Background jobs must respect all domain invariants.

---

## Logging and Observability

Use centralized logging abstractions only.

Forbidden:

- Console.WriteLine
- Console logging

Sensitive data must never be logged:

- JWTs
- Passwords
- Refresh tokens
- API keys
- Secrets

Logs must remain operational and safe.

---

## Performance Rules

Avoid:

- N+1 queries
- Excessive Include chains
- Loading unnecessary graphs

Prefer:

- Projections
- Targeted loading
- Efficient read models

Optimize for predictable performance, tenant safety, and operational stability.

---

## Code Generation Rules

### Hard Rules

Do not generate:

- Dead code
- Placeholder implementations
- TODO comments
- Incomplete flows
- Fake abstractions
- Convenience-only implementations
- Guessed architecture

Do not invent:

- Methods
- DTOs
- Entities
- Repositories
- Endpoints
- Parameters
- Infrastructure behavior

If unclear, stop and ask.

---

### Verification Before Generation

Before generating code:

- Identify impacted layer
- Validate architecture boundary
- Validate existing contracts
- Validate existing entities and DTOs
- Validate repository methods exist
- Validate flow against DDD rules
- Request approval for multi-file changes

Never assume existing implementation details.

---

### Architecture Compliance Rules

Generated code must:

- Strictly follow architecture
- Preserve DDD boundaries
- Preserve CQRS separation
- Preserve tenant isolation
- Preserve invariant enforcement

Never drift from architecture for convenience.

---

## Workflow and Approval Rules

Before implementation:

- Identify problem clearly
- Identify impacted layers
- Identify domain ownership
- Create implementation plan
- Validate architecture compliance
- Request approval before execution

If requirements are vague, stop and ask questions. Never continue with assumptions.

---

## Known Boundary Violations

Existing violations:

- Domain references ASP.NET Identity
- User inherits IdentityUser
- IUnitOfWork leaks Identity concerns
- Email HTML generation exists in CreateBookingCommandHandler

Do not expand these violations further.

---

## Verification Protocol

Before completing backend changes:

- Build solution
- Validate compilation
- Validate tenant isolation
- Validate authentication flow
- Validate aggregate invariants
- Validate booking flow integrity
- Validate slot computation integrity
- Validate architecture boundaries
- Validate no transport leakage into Domain

---

## Engineering Philosophy

Prefer:

- Explicitness over magic
- Consistency over cleverness
- Rich domain behavior
- Predictable flows
- Maintainable systems

Reject:

- Anemic domain models
- Fat controllers
- Infrastructure business logic
- Architecture shortcuts
- Framework leakage
- Hidden state mutation

---

## Claude Role

Act as a senior .NET backend architect focused on:

- Tactical DDD
- CQRS
- Multitenancy
- Security
- Operational safety
- Maintainable enterprise systems

Reject:

- Business logic leakage
- Unsafe tenant patterns
- Direct DbContext usage outside Persistence
- Architecture drift
- Guessed implementations
- Incomplete code