# ADR 0002. Structure backend with ASP.NET Core, MVCS, and DTOs

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

The application needs a backend capable of exposing a REST API, handling business rules for operational restrictions and digital signatures, and managing persistent data.

Putting routing, business logic, and database access inside controllers leads to bloated classes that are hard to read and test. Returning database entities directly from controllers also couples the public API contract to the internal database schema, risking accidental exposure of internal fields or circular serialization errors.

We need a clear architectural structure with static typing, built-in dependency injection, and separation between persistence entities, business logic, and public API contracts.

## Decision

We will use C# with .NET 10 and ASP.NET Core as our backend stack, structured using the Model-View-Controller-Service (MVCS) pattern with strict separation between Entity models and Data Transfer Objects (DTOs).

Controllers handle HTTP requests and responses, Services execute application and business logic, Entities model database persistence, and DTOs define public API contracts.

## Consequences

### What becomes easier

- C# static typing and compiler checks catch errors before runtime.
- Built-in ASP.NET Core infrastructure handles dependency injection, routing, middleware, and configuration without third-party frameworks.
- Controllers remain thin and focused on HTTP status codes, routing, and request validation.
- Business rules are isolated in Services, making them straightforward to unit test without mocking HTTP contexts.
- Database schemas can change without breaking public API contracts, and internal database properties stay hidden from clients.

### What becomes harder

- Adding a feature requires touching multiple layers (Controller, Service, Entity, DTO, and mapping code).
- Introduces more classes, interfaces, and boilerplate for simple CRUD operations.
- The team needs familiarity with C#, .NET 10 conventions, and dependency injection lifecycles.
- Developers must maintain discipline so business logic does not slip into controllers for quick convenience.

### What this commits us to

The backend depends on the .NET 10 runtime and ASP.NET Core. Moving to another backend platform later would require rewriting the API and server-side business logic.

## Alternatives considered

**Node.js with Express.** A lightweight JavaScript option that would unify language across frontend and backend. We rejected it because C# gives us stronger static typing for complex domain rules, built-in application infrastructure, and direct integration with Entity Framework Core.

**Java with Spring Boot.** A mature enterprise alternative with similar capabilities. We rejected it because the team preferred the modern .NET ecosystem, performance, and tooling for this project.

**Controller-only architecture (no service layer).** Writing business rules and database queries directly in controller actions. We rejected this because controllers quickly become bloated, difficult to test, and tightly coupled to persistence.

**Returning EF Core entities directly instead of DTOs.** Saves mapping code, but couples the public API to database schema and risks over-posting bugs or circular JSON serialization errors.

**Clean Architecture / Hexagonal.** Splitting domain, application, infrastructure, and presentation into separate projects and dependency-inverted layers. We rejected this as disproportionate for our current scope, adding project-boundary overhead without immediate benefit.

## Notes

Keep mappings between entities and DTOs explicit and predictable. Do not add extra abstraction layers (such as a generic repository over EF Core) unless they solve a concrete, identifiable problem.
