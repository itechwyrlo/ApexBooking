CLEAN ARCHITECTURE REFERENCE FOR CODEBASE ANALYSIS
LAYER DEPENDENCY RULE
Dependencies point inward only. Domain has zero outward dependencies.

Domain: no references to any other layer
Application: references Domain only
Persistence: references Domain only
Web API: references all three

If you see Domain importing from Application, Persistence, or Web API, flag it as a boundary violation.
WHAT EACH LAYER OWNS
Domain owns: entities, value objects, enums, domain service interfaces, repository interfaces. No EF Core. No SQL. No HTTP.
Application owns: commands, queries, handlers, DTOs, validators, mapping profiles. Orchestrates use cases. Never references concrete EF Core classes.
Persistence owns: DbContext, entity configurations, concrete repository implementations, UnitOfWork implementation. The only layer allowed to know about EF Core.
Web API owns: controllers or minimal API endpoints, global exception handler. Receives HTTP requests, dispatches to MediatR, returns responses. Nothing else.
AGGREGATE ROOT RULES
An Aggregate Root is the sole entry point for all operations on its child entities.

Child entities have no repository of their own
Child collections are private, exposed only as IReadOnlyCollection
All mutations go through the Aggregate Root's methods, never directly
Business rule validation runs inside those methods, not outside

If you see a repository for a child entity, flag it as an aggregate boundary violation.
REPOSITORY RULES
Create a repository only for an Aggregate Root.

Generic repository handles: Add, Update, Remove, GetByIdAsync, GetPagedAsync
Specialized repository handles: domain-specific queries the generic base cannot express
Repository interfaces are defined in Domain
Concrete implementations live in Persistence
Repositories are internal, instantiated only by UnitOfWork

If Application references a concrete repository or bypasses UnitOfWork, flag it.
DEPENDENCY INVERSION CHECKPOINTS

IUnitOfWork is defined in Domain, implemented in Persistence
Application depends on the interface, never the concrete class
Compile-time enforcement: boundary violations appear as missing project references, not just convention breaks

PERSISTENCE IGNORANCE TEST
Domain entities are plain classes with private fields, validation, and business logic. They contain no EF Core attributes, no database types, no infrastructure imports. If they pass this check, unit tests for domain rules run with no database required.
VIOLATION FLAGS TO RAISE

Domain importing from any other layer
Child entity with its own repository
Application referencing a concrete EF Core class
Direct mutation of a child collection from outside the Aggregate Root
Repository instantiated outside UnitOfWork
Business logic placed in controllers or handlers instead of domain entities