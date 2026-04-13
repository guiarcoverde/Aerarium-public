# CLAUDE.md - Aerarium

## Overview
Aerarium is a personal financial manager web application built by the Animus team.

## Tech Stack
- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 with PostgreSQL
- ASP.NET Identity with EF Core for user management
- Mediator for CQRS (source-generated, https://github.com/martinothamar/Mediator)
- FluentValidation for request validation
- Scalar for OpenAPI documentation
- Angular 21 (standalone, signals-first, zoneless)
- xUnit + FluentAssertions for testing

## Project Structure
- `src/Api/` - Endpoints, middleware, DI configuration
- `src/Application/` - Commands, queries, handlers, validators
- `src/Domain/` - Entities, value objects, enums, domain events
- `src/Infrastructure/` - EF Core, external services, repositories
- `src/Frontend/` - Angular frontend
- `tests/UnitTests/` - Domain and application layer tests
- `tests/IntegrationTests/` - API and database tests

## Commands

### Backend
- Build: `dotnet build`
- Test: `dotnet test`
- Run API: `dotnet run --project src/Api`
- Add Migration: `dotnet ef migrations add <n> -p src/Infrastructure -s src/Api`
- Update Database: `dotnet ef database update -p src/Infrastructure -s src/Api`
- Format: `dotnet format`

### Frontend
- Dev server: `cd src/Frontend && ng serve`
- Build: `cd src/Frontend && ng build`
- Test: `cd src/Frontend && ng test`
- Lint: `cd src/Frontend && ng lint`
- Generate component: `cd src/Frontend && ng g c features/<feature>/components/<n>`
- Generate service: `cd src/Frontend && ng g s features/<feature>/services/<n>`

## Architecture Rules
- Domain layer has ZERO external dependencies
- Application layer defines interfaces, Infrastructure implements them
- All database access goes through EF Core DbContext (no repository pattern)
- Use Mediator for all command/query handling
- API layer is thin - endpoint definitions only

## Authentication & Authorization
- ASP.NET Identity with EF Core for user management
- JWT Bearer tokens for API authentication
- Credentials stored in AWS SSM Parameter Store (not appsettings)
- Authorization via endpoint-level policies, not controller attributes

## Configuration
- Secrets come from AWS SSM Parameter Store in production
- Local development uses `appsettings.Development.json` (never commit secrets)
- Connection string key: `ConnectionStrings:Default`
- Never suggest hardcoded secrets or appsettings.json for sensitive values

## Error Handling
- Handlers return `Result<T>` - never throw for business logic
- Global exception middleware in Api layer handles unexpected exceptions
- Use ProblemDetails (RFC 7807) format for all error responses
- Map Result errors to appropriate HTTP status codes in endpoints

## Endpoint Conventions
- One file per feature/entity: `UsersEndpoints.cs`, `TransactionsEndpoints.cs`
- Group endpoints with `RouteGroupBuilder`
- Extension method pattern: `app.MapUserEndpoints()`
- No logic in endpoints - dispatch to Mediator immediately

## EF Core Conventions
- No lazy loading - use explicit `Include()` when needed
- Configurations via `IEntityTypeConfiguration<T>`, never Data Annotations
- DbContext is in Infrastructure layer
- Never expose `IQueryable` outside of handlers

## HTTP Clients
- Always use `IHttpClientFactory` - never instantiate `HttpClient` directly
- Named clients configured in DI (`Program.cs`)
- Typed clients preferred for external service integrations
- Retry/timeout policies via Polly configured on registration

### Example pattern
- Define typed client in Infrastructure layer
- Register with `builder.Services.AddHttpClient<IMyServiceClient, MyServiceClient>()`
- Inject typed client directly into handlers or services

---

## Frontend (Angular 21)

### Tech Stack
- Angular 21 (standalone components, signals-first, zoneless change detection)
- Vitest as default test runner
- TypeScript strict mode
- SCSS for styling

### Project Structure
- `src/Frontend/src/app/core/` - Singleton services, guards, interceptors, auth
- `src/Frontend/src/app/shared/` - Reusable components, pipes, directives
- `src/Frontend/src/app/features/` - Feature folders with components and services per domain (lazy loaded)
- `src/Frontend/src/app/models/` - Interfaces and types (DTOs mirror backend contracts)
- `src/Frontend/src/environments/` - Per-environment configuration (API base URL, feature flags)

### Angular 21 - Mandatory Patterns
- Standalone components exclusively - NEVER use NgModules
- Signals for reactive state (`signal()`, `computed()`, `effect()`) - prefer over RxJS for simple state
- Zoneless change detection - DO NOT include zone.js
- Native template control flow (`@if`, `@for`, `@switch`) - NEVER use `*ngIf`, `*ngFor`, `ngSwitch`
- Smart Styling with native `[class]` and `[style]` bindings - NEVER use `NgClass` or `NgStyle`
- Signal Forms (experimental) for new forms when appropriate
- `inject()` function for DI - NEVER use constructor injection in components

### State Management
- Signals for local component state
- Services with signals for shared state across components
- RxJS only for complex async streams (WebSockets, composed events)
- `resource()` / `httpResource()` for reactive HTTP calls

### API Integration
- HttpClient via typed services per feature in `core/`
- Functional interceptor for auth header (JWT) and global error handling
- Interfaces/types for all DTOs - NEVER use `any`
- Base URL configured via `environment.ts`
- Centralized HTTP error handling (401 → redirect to login, 403 → user feedback, 5xx → generic error message)

### Components
- Smart (container) components: fetch data, manage state, dispatch actions
- Dumb (presentational) components: receive data via `input()`, emit events via `output()`
- Use `input()` and `output()` (signal-based) - NEVER use `@Input()` / `@Output()` decorators
- One component per file
- Styles always scoped (SCSS in the component itself)

### Routing
- Lazy loading per feature with `loadComponent` / `loadChildren`
- Functional route guards (`CanActivateFn`, `CanDeactivateFn`)
- Functional resolvers when data needs to be preloaded

### Forms
- Signal Forms for new forms (experimental API from Angular 21)
- Reactive Forms for existing/complex forms until Signal Forms stabilizes
- NEVER use template-driven forms
- Client-side validation mirrors FluentValidation rules from the backend

### Patterns We Use (Frontend)
- Functional interceptors and guards (not class-based)
- Barrel exports (`index.ts`) per feature
- Strong typing everywhere - `strict: true` in tsconfig
- `takeUntilDestroyed()` or `DestroyRef` for RxJS subscription cleanup
- `async` pipe when using Observables in templates
- Lazy loading for all feature routes

### Patterns We DON'T Use (Frontend - Never Suggest)
- `any` type (always type explicitly)
- NgModules (everything is standalone)
- `*ngIf`, `*ngFor`, `ngSwitch` (use `@if`, `@for`, `@switch`)
- `NgClass`, `NgStyle` (use native `[class]`, `[style]` bindings)
- Constructor injection in components (use `inject()`)
- `@Input()` / `@Output()` decorators (use signal-based `input()` / `output()`)
- Manual `subscribe()` without cleanup
- zone.js
- Business logic in components (move to services)
- Loose global CSS (styles always scoped or in theme files)
- `new HttpClient()` or direct HTTP calls in components

### Testing (Frontend)
- Vitest as default test runner
- Component tests with TestBed for UI interactions
- Services tested in isolation with HttpClient mocks
- Naming: `[name].component.spec.ts`, `[name].service.spec.ts`
- Test naming pattern: `it('should [expected behavior] when [condition]')`

---

## Code Conventions

### Naming
- Commands: `Create[Entity]Command`, `Update[Entity]Command`
- Queries: `Get[Entity]Query`, `List[Entities]Query`
- Handlers: `[Command/Query]Handler`
- DTOs: `[Entity]Dto`, `Create[Entity]Request`

### Patterns We Use
- Primary constructors for DI
- Records for DTOs and commands
- `Result<T>` pattern for error handling (no exceptions for flow control)
- File-scoped namespaces
- Always pass `CancellationToken` to async methods

### Patterns We DON'T Use (Never Suggest)
- Repository pattern (use EF Core directly)
- AutoMapper (write explicit mappings)
- Exceptions for business logic errors
- Stored procedures
- `new HttpClient()` (always use `IHttpClientFactory`)

## Validation
- All request validation in FluentValidation validators
- Validators auto-registered via assembly scanning
- Validation runs in Mediator pipeline behavior

## Testing
- Unit tests: Domain logic and handlers
- Integration tests: Full API endpoint testing with WebApplicationFactory
- Use FluentAssertions for readable assertions
- Test naming: `[Method]_[Scenario]_[ExpectedResult]`

## Git Workflow
- Branch naming: `feature/`, `bugfix/`, `hotfix/`
- Commit format: `type: description` (feat, fix, refactor, test, docs)
- Commits should always be grouped
- Always create a branch before changes
- Run tests before committing

## Architecture Decisions
- **No Repository pattern**: EF Core DbContext is the abstraction - adding a repository on top adds indirection with no benefit
- **No AutoMapper**: explicit mapping keeps code readable, refactorable, and avoids magic
- **Source-generated Mediator**: prefer compile-time over runtime reflection (martinothamar/Mediator, not MediatR)
- **No exceptions for flow control**: `Result<T>` makes error paths explicit and testable
- **No Data Annotations**: all EF Core config via `IEntityTypeConfiguration<T>` for separation of concerns
- **Standalone-only Angular**: NgModules are legacy - standalone components reduce boilerplate and simplify lazy loading
- **Signals over RxJS**: Signals are Angular 21's primary reactive model - RxJS only when complex streams justify it
- **Zoneless change detection**: better performance, no zone.js overhead, granular updates via signals

## When in Doubt
- Prefer boring, explicit code over clever abstractions
- If a pattern isn't listed here, ask before implementing
- Smaller PRs preferred - one feature per branch
- On the frontend, always use Angular 21 APIs (signals, standalone, zoneless) - never legacy patterns
