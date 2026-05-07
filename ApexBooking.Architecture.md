Architecture Overview
Dependencies always point inward toward the Domain layer.
1. Framework.Core / Shared Kernel
Houses common types, constants, and helper classes used across all projects. Has no dependencies. All other layers depend on it. Contains IAggregateRoot, and BusinessRuleBrokenException.
2. GenericRepository.Abstractions
Defines the IGenericRepository<TEntity> contract. Constrains data operations to Aggregate Roots only. Has no EF Core dependency, keeping the contract framework-agnostic.
3. GenericRepository.EntityFramework
Implements IGenericRepository<TEntity> using EF Core. Exposes DbContext as protected so specialized repositories inherit and extend it. Depends on GenericRepository.Abstractions.
4. Core.Domain
The heart of the application. Contains Entities, Value Objects, Aggregate Roots, Domain Services, and Repository Interfaces. Depends only on Shared Kernel and GenericRepository.Abstractions. Business rules stay isolated from database or ORM concerns.
5. Core.Persistence
Handles data storage. Contains EF Core Fluent API mappings, DbContext, migrations, and concrete Repository implementations including UnitOfWork. Depends on Core.Domain and GenericRepository.EntityFramework. Swap databases or update ORMs here without touching business logic.
6. Core.Application
Orchestrates use cases using MediatR. Contains Commands, Queries, Validators, and AutoMapper profiles. Depends on Core.Domain. Handlers are thin by design; all business rules stay in the Domain.
7. Infrastructure
Handles external service integrations. Contains the ExternalTaxProvider which implements ITaxCalculationService from Core.Domain. Depends only on Core.Domain.
8. Web API
The entry point for external systems. Routes HTTP requests to the Application layer via MediatR. Registers all dependencies in Program.cs. Depends on all inner layers.
Dependency Flow
Shared Kernel → GenericRepository.Abstractions → Core.Domain → Core.Persistence → Core.Application → Web API
GenericRepository.EntityFramework sits alongside Persistence, pointing inward to GenericRepository.Abstractions. Infrastructure sits alongside Persistence, pointing inward to Core.Domain.