# ADR 0003. Use PostgreSQL and Entity Framework Core for persistence

**Status.** Accepted
**Date.** 2026-09-17
**Deciders.** Group 5
**Related backlog items.** MET-C-029, Initial Project Infrastructure & Scaffolding

## Context

The application requires persistent storage for operational restrictions, signatures, audit events, and user records. This domain data has strict relational structure, requiring foreign key constraints, transactions, and structured querying.

Because the backend runs on .NET 10, integration with Entity Framework Core is a natural fit. We also need an open-source relational database that runs reliably in local Docker containers and production hosting.

Working with database records through strongly typed C# models and LINQ is preferable to writing and maintaining manual SQL strings for routine operations.

## Decision

We will use PostgreSQL as the database and Entity Framework Core with the Npgsql provider as the ORM.

Entity Framework Core maps C# entity classes to PostgreSQL tables and provides database access through LINQ and migrations.

## Consequences

### What becomes easier

- Database operations and queries are expressed directly in C# using LINQ.
- Entity Framework Core maps C# entity classes and navigation properties to relational tables and foreign keys automatically.
- EF Core migrations provide automated, version-controlled schema evolution that can run in CI or at startup.
- Works identically in local Docker Compose environments and deployed production databases.

### What becomes harder

- Developers need to understand both relational database principles and EF Core mapping conventions.
- Inefficient LINQ queries can generate poor SQL (such as N+1 queries), requiring developers to inspect generated SQL during performance troubleshooting.
- Advanced PostgreSQL-specific features are not always directly abstracted by EF Core and may require raw SQL escapes or specific configuration.

### What this commits us to

The application depends on PostgreSQL and the Npgsql EF Core provider. Replacing PostgreSQL with another database requires updating configuration, schema migrations, and queries that rely on PostgreSQL-specific features.

## Alternatives considered

**Raw SQL with Npgsql.** Direct SQL gives full control over queries and PostgreSQL-specific features. We rejected it because manually writing and maintaining SQL for standard CRUD operations adds development overhead and loses the compile-time safety of typed C# models.

**Microsoft SQL Server.** Integrates tightly with .NET. We chose PostgreSQL because it is open source, runs cleanly in lightweight Docker containers, and satisfies all relational requirements without licensing constraints.

**MongoDB.** A document database offers schema flexibility, but our domain data is inherently relational, requiring referential integrity, constraints, multi-table transactions, and relational queries.

## Notes

Monitor database query performance as data volume grows. Inspect generated SQL queries and add database indexes for performance-critical endpoints.
