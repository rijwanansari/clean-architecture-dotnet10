# CleanArchitecture

A production-grade .NET 10 Web API built with **Clean Architecture**, custom **CQRS** (no MediatR), **FluentValidation**, and **Entity Framework Core 10**.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Tech Stack](#tech-stack)
- [CQRS — Without MediatR](#cqrs--without-mediatr)
- [CQRS Pipeline](#cqrs-pipeline)
- [Validation Pipeline](#validation-pipeline)
- [Error Handling](#error-handling)
- [Getting Started](#getting-started)
- [Database Migrations](#database-migrations)
- [Running Tests](#running-tests)

---

## Architecture Overview

The solution follows **Clean Architecture** principles — dependency flow always points inward. Outer layers depend on inner layers; inner layers never depend on outer layers.

```
┌─────────────────────────────────────┐
│            WebApi (HTTP)            │  ← Controllers, Middleware, DI wiring
├─────────────────────────────────────┤
│         Infrastructure              │  ← EF Core, Repositories, DbContext
├─────────────────────────────────────┤
│          Application                │  ← CQRS, Handlers, Validators, DTOs
├─────────────────────────────────────┤
│            Domain                   │  ← Entities, Interfaces, Value Objects
└─────────────────────────────────────┘
```

**Dependency rule:** Domain ← Application ← Infrastructure ← WebApi

---

## Project Structure

```
CleanArch10/
├── src/
│   ├── CleanArchitecture.Domain/
│   │   ├── Common/                     # BaseEntity, AuditableEntity, BaseEvent
│   │   ├── Entities/                   # Product
│   │   ├── Enums/                      # ProductStatus
│   │   ├── Exceptions/                 # DomainException, NotFoundException
│   │   ├── Interfaces/                 # IRepository<T>, IProductRepository, IUnitOfWork
│   │   └── ValueObjects/               # Money
│   │
│   ├── CleanArchitecture.Application/
│   │   ├── Common/
│   │   │   ├── CQRS/                   # ICommand, IQuery, ICommandHandler, IQueryHandler, IDispatcher, Unit
│   │   │   ├── Dispatching/            # Dispatcher, ValidatingDispatcher
│   │   │   ├── Exceptions/             # ValidationException
│   │   │   └── Models/                 # PaginatedList
│   │   ├── Products/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateProduct/      # Command, Handler, Validator
│   │   │   │   ├── UpdateProduct/      # Command, Handler, Validator
│   │   │   │   └── DeleteProduct/      # Command, Handler
│   │   │   └── Queries/
│   │   │       ├── GetProducts/        # Query, Handler, ProductDto
│   │   │       └── GetProductById/     # Query, Handler
│   │   └── DependencyInjection.cs      # Auto-registers handlers + validators
│   │
│   ├── CleanArchitecture.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── ApplicationDbContextInitialiser.cs
│   │   │   ├── Configurations/         # EF entity type configurations
│   │   │   ├── Migrations/             # EF Core migrations
│   │   │   └── Repositories/           # GenericRepository, ProductRepository
│   │   └── DependencyInjection.cs      # Registers DbContext, repositories, UoW
│   │
│   └── CleanArchitecture.WebApi/
│       ├── Controllers/                # ProductsController
│       ├── Contracts/Requests/         # Request DTOs (UpdateProductRequest)
│       ├── Middleware/                 # ExceptionHandlingMiddleware
│       ├── Properties/
│       ├── appsettings.json
│       └── Program.cs
│
└── tests/
    └── CleanArchitecture.Application.UnitTests/
        └── Products/Commands/          # CreateProductCommandHandlerTests
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 10 / ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server (LocalDB for dev) |
| Validation | FluentValidation 11 |
| CQRS | Custom dispatcher (no MediatR) |
| Testing | xUnit + NSubstitute |
| API Docs | Swagger / OpenAPI |

---

## CQRS — Without MediatR

**CQRS (Command Query Responsibility Segregation)** is a pattern that separates every operation into one of two kinds:

| Kind | Intent | Side effects? |
|---|---|---|
| **Command** | Change state — create, update, delete | Yes — writes to the database |
| **Query** | Read state — fetch one or many | No — read-only |

This separation makes each use-case a single-purpose, independently testable unit of work.

### Why not MediatR?

#### MediatR is no longer fully open source

MediatR, authored by Jimmy Bogard, has historically been the go-to CQRS/mediator library in the .NET ecosystem. However, as of **MediatR v12+**, the licensing model changed:

- The core library remains MIT-licensed, but **MediatR.Contracts was separated** into a paid/commercial offering under a different licence for certain commercial use cases.
- The author has signalled a move toward a **commercial support and licensing model**, following the same path taken by other popular OSS libraries (e.g. Hangfire, ImageSharp).
- This introduces **licence compliance risk** for commercial products — legal review may be required before use in enterprise or SaaS products.
- Future versions may shift further toward a commercial model, making it a **long-term vendor dependency risk**.

> **This is a significant concern for enterprise projects.** Adopting an external library for a pattern you can implement in ~100 lines of code introduces unnecessary risk when that library's licence and future are uncertain.

## CQRS Pipeline

Commands and queries are dispatched via a two-layer dispatcher — **no MediatR dependency**.

### Command flow (write side)

```
Controller
  └─► IDispatcher.SendAsync(command)
        └─► ValidatingDispatcher          ← runs FluentValidation
              └─► Dispatcher              ← resolves ICommandHandler<T, R> from DI
                    └─► Handler.HandleAsync()
                          ├─► Domain logic (Entity.Create / Update)
                          ├─► Repository.AddAsync / Update
                          └─► UnitOfWork.SaveChangesAsync()
```

### Query flow (read side)

```
Controller
  └─► IDispatcher.QueryAsync(query)
        └─► ValidatingDispatcher          ← passes through, no validation on queries
              └─► Dispatcher              ← resolves IQueryHandler<T, R> from DI
                    └─► Handler.HandleAsync()
                          ├─► Repository.GetAllAsync / GetByIdAsync
                          └─► map Domain entities → DTOs
```

### Key contracts

```csharp
// A command that returns TResult
public interface ICommand<TResult> { }

// A command with no meaningful result (returns Unit)
public interface ICommand : ICommand<Unit> { }

// A query that returns TResult
public interface IQuery<TResult> { }

// Handler contracts
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
public interface IQueryHandler<in TQuery, TResult>     where TQuery   : IQuery<TResult>
```

Handler resolution uses **runtime generic type construction** — handlers are discovered and registered automatically via assembly scanning in `AddApplication()`. No manual registration is required when adding new handlers.

---

## Validation Pipeline

Each command can have a corresponding `AbstractValidator<TCommand>`. The `ValidatingDispatcher` resolves all registered validators before the handler executes.

Example — `CreateProductCommandValidator`:

```csharp
RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
RuleFor(x => x.Price).GreaterThan(0);
RuleFor(x => x.Currency).NotEmpty().Length(3);
RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
```

If any rule fails, a `ValidationException` is thrown **before the handler runs**. No handler code executes on invalid input.

---

## Error Handling

All unhandled exceptions are caught by `ExceptionHandlingMiddleware` and mapped to structured HTTP responses:

| Exception | HTTP Status | Description |
|---|---|---|
| `ValidationException` | 400 Bad Request | FluentValidation failure — returns error dictionary |
| `NotFoundException` | 404 Not Found | Entity not found by ID |
| `DomainException` | 422 Unprocessable Entity | Business rule violation |
| Any other | 500 Internal Server Error | Unexpected error — safe message returned |

Controllers contain **no try/catch** — all error translation is centralised in the middleware.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dot.net)
- SQL Server or LocalDB

### Clone and build

```bash
git clone <repo-url>
cd CleanArch10
dotnet build CleanArchitecture.slnx
```

### Configure connection string

Edit `src/CleanArchitecture.WebApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleanArchitectureDb;Trusted_Connection=True;"
  }
}
```

### Run the API

```bash
dotnet run --project src/CleanArchitecture.WebApi
```

Swagger UI is available at `https://localhost:{port}/swagger` in Development mode.

---

## Database Migrations

The `ApplicationDbContextInitialiser` automatically applies pending migrations on startup in Development.

### Add a new migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.WebApi \
  --output-dir Data/Migrations
```

### Apply migrations manually

```bash
dotnet ef database update \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.WebApi
```

### Remove last migration (if not yet applied)

```bash
dotnet ef migrations remove \
  --project src/CleanArchitecture.Infrastructure \
  --startup-project src/CleanArchitecture.WebApi
```

---

## Running Tests

```bash
dotnet test CleanArchitecture.slnx
```

Unit tests are in `tests/CleanArchitecture.Application.UnitTests` and cover handler behaviour using NSubstitute mocks — no database required.
