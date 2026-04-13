# AGENTS.md – Aerarium

## Architecture

4-layer .NET backend + Angular 21 frontend. Dependency flow: `Api → Application → Domain ← Infrastructure`.

```
Domain      – Zero external deps. Entities with private setters, factory methods returning Result<T>.
Application – CQRS handlers, IAppDbContext interface, FluentValidation validators, ICategoryLocalizer.
Infrastructure – AppDbContext (implements IAppDbContext), TokenService, EF configurations.
Api         – Thin endpoints only. Dispatch to IMediator immediately. Register in Program.cs.
```

## Key Workflows

```bash
docker compose up -d                        # Start PostgreSQL (localhost:5432, db/user/pass: aerarium / aerarium_dev)
make migrate                                # Apply EF migrations
make run                                    # API at http://localhost:5281 | Scalar UI: /scalar/v1
make fe                                     # Angular dev server at http://localhost:4200
make migration name=AddFoo                  # Create a new EF migration
make test                                   # Run all backend tests
```

## Adding a New Feature (Backend)

1. **Domain** – add entity in `src/Domain/Entities/`; factory method returns `Result<T>`.
2. **Application** – create `src/Application/<Feature>/<Action>/` with `Command/Query` record (implements `ICommand<Result<T>>` or `IQuery<Result<T>>`), `Handler`, and `Validator`.
3. **Infrastructure** – add `IEntityTypeConfiguration<T>` in `src/Infrastructure/Persistence/Configurations/`.
4. **Api** – add `DbSet<T>` to `IAppDbContext` and `AppDbContext`; create `src/Api/Endpoints/<Feature>Endpoints.cs`; call `app.Map<Feature>Endpoints()` in `Program.cs`.

## Critical Patterns

**Result\<T\>** (never throw for business logic):
```csharp
// Domain entity
public static Result<Transaction> Create(...) {
    if (amount <= 0) return Result<Transaction>.Failure("Amount must be greater than zero.");
    return new Transaction { ... };
}
// Handler checks
if (result.IsFailure) return Result<TransactionDto>.Failure(result.Error!);
// Endpoint maps
return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: result.Error, statusCode: 400);
```

**Mediator** – source-generated (`martinothamar/Mediator`, not MediatR). Commands use `ICommand<T>` / `ICommandHandler<,>`, queries use `IQuery<T>` / `IQueryHandler<,>`. Handlers return `ValueTask<T>`.

**ValidationBehavior** – automatically runs all `AbstractValidator<TCommand>` in the pipeline. Validators return `Result<T>.Failure(...)` instead of throwing when the handler returns `Result<T>`.

**TransactionCategory encoding** – Income categories: 100–199; Expense categories: 200–299. Enforced in both domain (`IsValidCategoryForType`) and validator.

**EF Core** – use `IAppDbContext` in handlers (no repositories). Add `AsNoTracking()` for read-only queries. All column config via `IEntityTypeConfiguration<T>` in Infrastructure, never Data Annotations.

**User isolation** – handlers inject `ICurrentUserService` (implemented in Api layer via `ClaimTypes.NameIdentifier`). Always filter queries by `currentUser.UserId`.

**Localization** – `ICategoryLocalizer.GetDisplayName(category)` translates `TransactionCategory` enum values. Resources in `src/Application/Resources/Categories.resx` (pt-BR default) and `Categories.en.resx`.

## Adding a New Feature (Frontend)

- Feature folder: `src/Frontend/src/app/features/<feature>/`
- Add DTO interfaces to `src/Frontend/src/app/models/`
- Services use `inject(HttpClient)` and `environment.apiUrl` for base URL
- Register lazy routes in `app.routes.ts` using `loadComponent`
- Auth state lives in `AuthService` (signals: `isAuthenticated`, `token`, `email`); JWT stored in `localStorage` under keys `aerarium_token` / `aerarium_email`
- `authInterceptor` attaches `Authorization: Bearer <token>` automatically

## What NOT to Do

- No repository pattern – use `IAppDbContext` (EF Core DbContext) directly in handlers
- No AutoMapper – write explicit DTO mappings in handlers
- No `new HttpClient()` – use injected `HttpClient` in Angular services
- No `*ngIf` / `*ngFor` / `NgClass` / `@Input()` / `@Output()` / NgModules – use Angular 21 APIs
- No constructor injection in Angular components – use `inject()`
- No exceptions for business logic – use `Result<T>.Failure(...)`
- No secrets in `appsettings.json` – use `appsettings.Development.json` locally (never commit)

