# Aerarium

Personal finance manager built by the Animus team.

## Tech Stack

### Backend
- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 with PostgreSQL
- ASP.NET Identity for authentication (JWT Bearer)
- [Mediator](https://github.com/martinothamar/Mediator) (source-generated) for CQRS
- FluentValidation for request validation
- Scalar for OpenAPI documentation
- xUnit + FluentAssertions for testing

### Frontend
- Angular 21 (standalone components, signals, zoneless)
- TypeScript strict mode
- SCSS for styling
- Vitest as the test runner

## Project Structure

```
src/
├── Api/              # Endpoints, middleware, DI configuration
├── Application/      # Commands, queries, handlers, validators
├── Domain/           # Entities, value objects, enums
├── Infrastructure/   # EF Core, external services
└── Frontend/         # Angular application
    └── src/
        └── app/
            ├── core/         # Services, guards, interceptors
            ├── features/     # Feature modules (lazy loaded)
            ├── models/       # Interfaces and types (DTOs)
            └── shared/       # Reusable components

tests/
├── UnitTests/        # Domain and application tests
└── IntegrationTests/ # API and database tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 24+](https://nodejs.org/) and npm
- [Docker](https://www.docker.com/) (for PostgreSQL)

## Getting Started

### 1. Start the database

```bash
docker compose up -d
```

This starts a PostgreSQL container on port `5432` with the following credentials:

| Field    | Value           |
|----------|-----------------|
| Host     | localhost       |
| Port     | 5432            |
| Database | aerarium        |
| Username | aerarium        |
| Password | aerarium_dev    |

### 2. Apply migrations

```bash
make migrate
```

### 3. Install frontend dependencies

```bash
make fe-install
```

### 4. Run the application

In separate terminals:

```bash
# Terminal 1 — API (.NET)
make run

# Terminal 2 — Frontend (Angular)
make fe
```

- Frontend: `http://localhost:4200`
- API: `http://localhost:5281`
- Scalar UI: `http://localhost:5281/scalar/v1`

## Useful Commands

| Command | Description |
|---------|-------------|
| `make build` | Build the backend |
| `make run` | Run the API |
| `make test` | Run all tests |
| `make migration name=Name` | Create a migration |
| `make migrate` | Apply migrations |
| `make fmt` | Format the code |
| `make fe-install` | Install frontend dependencies |
| `make fe` | Run the Angular dev server |
| `make fe-build` | Production build of the frontend |

## Git Workflow

- Branches: `feature/`, `bugfix/`, `hotfix/`
- Commits: `type: description` (feat, fix, refactor, test, docs)

## License

Private project — Animus.
