# Implementation Plan: Transactions, Categories & Dashboard

## Context
Aerarium is a greenfield personal finance app. The project structure is scaffolded (6 projects, NuGet packages, build passing) but has no business logic yet. We need to implement the first feature: Transactions (income/expenses) with predefined categories and a dashboard summary. Categories must be internationalized via `.resx` files (primary: pt-BR).

## Design Decisions

1. **Categories as enum, not entity** — Categories are predefined and fixed. Using `TransactionCategory` enum in Domain avoids a separate table and joins. Numbering: 100–199 for income, 200–299 for expenses (easy type validation, room for growth).

2. **`.resx` files in Application** — Domain must have zero dependencies. Resource files need `Microsoft.Extensions.Localization`, so they live in Application.

3. **Amount as `decimal(18,2)`** — No `Money` value object yet. Single-currency app doesn't need it.

4. **User scoping via `ICurrentUserService`** — Interface in Application, implemented in Api reading from JWT claims. All handlers use this, never accept userId in request body.

5. **`Guid.CreateVersion7()`** — .NET 10 time-ordered UUIDs for better index performance.

---

## Phase 1: Domain Layer

### Files to create:

**`src/Domain/Enums/TransactionType.cs`**
- `Income = 1`, `Expense = 2`

**`src/Domain/Enums/TransactionCategory.cs`**
- Income (100s): `Salary=100, Bonus=101, Loan=102, Investment=103, ExtraIncome=104, Gift=105, Pix=106, BankTransfer=107, OtherIncome=108`
- Expense (200s): `Housing=200, Education=201, Electronics=202, Leisure=203, OtherExpense=204, Restaurant=205, Health=206, Services=207, Grocery=208, Transportation=209, Clothing=210, Travel=211`

**`src/Domain/Entities/Transaction.cs`**
- Properties: `Id (Guid)`, `UserId (string)`, `Amount (decimal)`, `Description (string)`, `Date (DateOnly)`, `Type (TransactionType)`, `Category (TransactionCategory)`, `CreatedAt (DateTime)`, `UpdatedAt (DateTime?)`
- Private parameterless constructor for EF Core
- `static Result<Transaction> Create(...)` — factory method with validations (amount > 0, description not empty, category valid for type)
- `Result<Transaction> Update(...)` — mutation method, sets UpdatedAt
- `private static bool IsValidCategoryForType(...)` — checks category number range matches type

---

## Phase 2: Application Layer

### Common infrastructure:

**`src/Application/Common/ICurrentUserService.cs`**
- `string UserId { get; }`

**`src/Application/Common/PagedResult.cs`**
- `record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)`

**`src/Application/Common/Behaviors/ValidationBehavior.cs`**
- `IPipelineBehavior<TMessage, TResponse>` that runs FluentValidation before handler
- Short-circuits with `Result<T>.Failure(...)` on validation errors

### i18n:

**`src/Application/Resources/Categories.resx`** (default = pt-BR)
- Keys = enum names, Values = pt-BR display names (Salário, Bonificação, Empréstimo, etc.)

**`src/Application/Resources/Categories.en.resx`**
- Keys = enum names, Values = English display names (Salary, Bonus, Loan, etc.)

**`src/Application/Common/ICategoryLocalizer.cs`**
- `string GetDisplayName(TransactionCategory category)`

**`src/Application/Common/CategoryLocalizer.cs`**
- Uses `IStringLocalizer<Categories>` to resolve names

### Transactions — DTOs:

**`src/Application/Transactions/TransactionDto.cs`**
- `record TransactionDto(Guid Id, decimal Amount, string Description, DateOnly Date, TransactionType Type, TransactionCategory Category, string CategoryDisplayName, DateTime CreatedAt, DateTime? UpdatedAt)`

### Transactions — Create:

**`src/Application/Transactions/Create/CreateTransactionCommand.cs`**
- `record CreateTransactionCommand(decimal Amount, string Description, DateOnly Date, TransactionType Type, TransactionCategory Category) : ICommand<Result<TransactionDto>>`

**`src/Application/Transactions/Create/CreateTransactionValidator.cs`**
- Amount > 0, Description not empty (max 500), Type/Category are defined enum values, Category valid for Type

**`src/Application/Transactions/Create/CreateTransactionHandler.cs`**
- Injects `AppDbContext`, `ICurrentUserService`, `ICategoryLocalizer`
- Calls `Transaction.Create(...)`, adds to DbContext, saves, returns DTO

### Transactions — Get by ID:

**`src/Application/Transactions/GetById/GetTransactionQuery.cs`**
- `record GetTransactionQuery(Guid Id) : IQuery<Result<TransactionDto>>`

**`src/Application/Transactions/GetById/GetTransactionHandler.cs`**
- Queries by Id AND UserId, returns failure if not found

### Transactions — List:

**`src/Application/Transactions/List/ListTransactionsQuery.cs`**
- `record ListTransactionsQuery(int? Month, int? Year, TransactionType? Type, int Page = 1, int PageSize = 20) : IQuery<Result<PagedResult<TransactionDto>>>`

**`src/Application/Transactions/List/ListTransactionsHandler.cs`**
- Filters by UserId, optional Month/Year/Type, orders by Date desc, paginates

### Transactions — Update:

**`src/Application/Transactions/Update/UpdateTransactionCommand.cs`**
- `record UpdateTransactionCommand(Guid Id, decimal Amount, string Description, DateOnly Date, TransactionType Type, TransactionCategory Category) : ICommand<Result<TransactionDto>>`

**`src/Application/Transactions/Update/UpdateTransactionValidator.cs`**

**`src/Application/Transactions/Update/UpdateTransactionHandler.cs`**
- Loads by Id + UserId, calls entity.Update(...), saves

### Transactions — Delete:

**`src/Application/Transactions/Delete/DeleteTransactionCommand.cs`**
- `record DeleteTransactionCommand(Guid Id) : ICommand<Result<bool>>`

**`src/Application/Transactions/Delete/DeleteTransactionHandler.cs`**

### Dashboard:

**`src/Application/Dashboard/DashboardSummaryDto.cs`**
- `record DashboardSummaryDto(decimal TotalIncome, decimal TotalExpenses, decimal Balance, IReadOnlyList<CategoryBreakdownDto> IncomeByCategory, IReadOnlyList<CategoryBreakdownDto> ExpenseByCategory)`
- `record CategoryBreakdownDto(TransactionCategory Category, string CategoryDisplayName, decimal Total, int Count)`

**`src/Application/Dashboard/GetDashboardSummaryQuery.cs`**
- `record GetDashboardSummaryQuery(int Month, int Year) : IQuery<Result<DashboardSummaryDto>>`

**`src/Application/Dashboard/GetDashboardSummaryHandler.cs`**
- Groups by Category, computes totals via EF Core aggregation, filtered by UserId + Month/Year

### DI Registration:

**`src/Application/DependencyInjection.cs`**
- `AddApplication(this IServiceCollection)` — registers validators, CategoryLocalizer, validation behavior

---

## Phase 3: Infrastructure Layer

**`src/Infrastructure/Persistence/Configurations/TransactionConfiguration.cs`**
- Table: `Transactions`
- `Amount`: `HasPrecision(18, 2)`
- `Description`: `HasMaxLength(500)`
- `Type`, `Category`: `HasConversion<int>()`
- `UserId`: required, `HasMaxLength(450)`
- Indexes: `UserId`, composite `(UserId, Date)`

**Modify: `src/Infrastructure/Persistence/AppDbContext.cs`**
- Add `DbSet<Transaction> Transactions { get; set; }`

**`src/Infrastructure/DependencyInjection.cs`**
- `AddInfrastructure(this IServiceCollection, IConfiguration)` — registers AppDbContext with Npgsql, Identity services

**Migration**: `dotnet ef migrations add InitialTransactions -p src/Infrastructure -s src/Api`

---

## Phase 4: API Layer

**`src/Api/Services/CurrentUserService.cs`**
- Implements `ICurrentUserService`, reads `ClaimTypes.NameIdentifier` from `IHttpContextAccessor`

**`src/Api/Contracts/CreateTransactionRequest.cs`**
- `record CreateTransactionRequest(decimal Amount, string Description, DateOnly Date, TransactionType Type, TransactionCategory Category)`

**`src/Api/Contracts/UpdateTransactionRequest.cs`**
- Same shape as create

**`src/Api/Endpoints/TransactionsEndpoints.cs`**
```
POST   /api/transactions          → 201 Created
GET    /api/transactions          → 200 OK (paged)
GET    /api/transactions/{id}     → 200 OK
PUT    /api/transactions/{id}     → 200 OK
DELETE /api/transactions/{id}     → 204 No Content
```
- All require authorization
- Map `Result<T>` failures → ProblemDetails (404 for not found, 400 for validation)

**`src/Api/Endpoints/DashboardEndpoints.cs`**
```
GET /api/dashboard/summary?month=3&year=2026 → 200 OK
```

**`src/Api/Endpoints/CategoriesEndpoints.cs`**
```
GET /api/categories?type=income  → 200 OK (localized names)
GET /api/categories?type=expense → 200 OK (localized names)
```

**`src/Api/Middleware/GlobalExceptionHandler.cs`**
- Implements `IExceptionHandler`, returns 500 ProblemDetails, never leaks stack traces

**Modify: `src/Api/Program.cs`**
- `builder.Services.AddApplication()`
- `builder.Services.AddInfrastructure(builder.Configuration)`
- Register `ICurrentUserService`, `IHttpContextAccessor`
- `builder.Services.AddMediator()`
- Add JWT Bearer authentication
- Add localization services + middleware
- Map all endpoint groups
- Add exception handler

### NuGet packages to add:
- `src/Application`: `Microsoft.Extensions.Localization.Abstractions`
- `src/Api`: `Microsoft.AspNetCore.Authentication.JwtBearer`
- `tests/IntegrationTests`: `Testcontainers.PostgreSql`

---

## Phase 5: Testing

### Unit Tests (`tests/UnitTests/`):

**`Domain/Entities/TransactionTests.cs`**
- `Create_ValidInputs_ReturnsSuccess`
- `Create_ZeroAmount_ReturnsFailure`
- `Create_NegativeAmount_ReturnsFailure`
- `Create_EmptyDescription_ReturnsFailure`
- `Create_IncomeCategoryWithExpenseType_ReturnsFailure`
- `Create_ExpenseCategoryWithIncomeType_ReturnsFailure`
- `Update_ValidInputs_UpdatesPropertiesAndSetsUpdatedAt`

**`Application/Transactions/CreateTransactionHandlerTests.cs`**
- `Handle_ValidCommand_CreatesAndReturnsDto`
- `Handle_InvalidCategory_ReturnsFailure`

**`Application/Transactions/CreateTransactionValidatorTests.cs`**
- `Validate_EmptyDescription_HasError`
- `Validate_ZeroAmount_HasError`
- `Validate_ValidCommand_NoErrors`

**`Application/Dashboard/GetDashboardSummaryHandlerTests.cs`**
- `Handle_WithTransactions_ReturnsCorrectTotals`
- `Handle_NoTransactions_ReturnsZeros`

### Integration Tests (`tests/IntegrationTests/`):

**`Infrastructure/CustomWebApplicationFactory.cs`**
- Uses Testcontainers for real PostgreSQL
- Seeds test user

**`Endpoints/TransactionEndpointTests.cs`**
- `CreateTransaction_ValidRequest_Returns201`
- `CreateTransaction_Unauthenticated_Returns401`
- `GetTransaction_OtherUsersTransaction_Returns404`
- `ListTransactions_FilterByMonth_ReturnsFiltered`
- `DeleteTransaction_Existing_Returns204`

**`Endpoints/DashboardEndpointTests.cs`**
- `GetSummary_MixedTransactions_ReturnsCorrectBalance`
- `GetSummary_NoTransactions_ReturnsZeros`

---

## Implementation Order

| # | Layer | Task |
|---|-------|------|
| 1 | Domain | Enums: `TransactionType`, `TransactionCategory` |
| 2 | Domain | Entity: `Transaction` with factory + update + validation |
| 3 | Application | `ICurrentUserService`, `PagedResult<T>` |
| 4 | Application | `.resx` files + `ICategoryLocalizer` + `CategoryLocalizer` |
| 5 | Application | `TransactionDto` |
| 6 | Application | `ValidationBehavior` pipeline |
| 7 | Application | Create command + validator + handler |
| 8 | Application | Get query + handler |
| 9 | Application | List query + handler |
| 10 | Application | Update command + validator + handler |
| 11 | Application | Delete command + handler |
| 12 | Application | Dashboard DTOs + query + handler |
| 13 | Application | `DependencyInjection.cs` |
| 14 | Infrastructure | `TransactionConfiguration` |
| 15 | Infrastructure | Update `AppDbContext` with DbSet |
| 16 | Infrastructure | `DependencyInjection.cs` |
| 17 | Infrastructure | EF Core migration |
| 18 | Api | `CurrentUserService`, `GlobalExceptionHandler` |
| 19 | Api | Request contracts |
| 20 | Api | Endpoint files (Transactions, Dashboard, Categories) |
| 21 | Api | Update `Program.cs` (DI, auth, localization, endpoints) |
| 22 | Tests | Domain unit tests |
| 23 | Tests | Handler + validator unit tests |
| 24 | Tests | Integration test factory + endpoint tests |

## Verification
1. `dotnet build` — zero errors
2. `dotnet test` — all tests pass
3. `dotnet run --project src/Api` — API starts, Scalar UI shows all endpoints
4. Manual test: POST a transaction, GET it back, GET dashboard summary
