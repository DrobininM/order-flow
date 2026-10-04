# order-flow microservices

A demonstration online store built on a microservices architecture. The repository contains two independently deployable services — **OrderService** (authentication, products, orders) and **PaymentService** (payment processing) — that communicate asynchronously via Apache Kafka. Each solution follows the principles of **Clean Architecture** and the **CQRS** pattern.

Implementation details of each service are not duplicated in this file — see the documentation linked in the [Documentation](#documentation) section.

## Architecture

- **Two independent solutions** (`OrderService.sln`, `PaymentService.sln`), each with its own PostgreSQL database and its own `docker-compose.yml`.
- **Clean Architecture** in each service: layers `Domain` → `Application` → `Infrastructure` → `Presentation`, with dependencies pointing inward.
- **CQRS** via MediatR: commands/queries with separate handlers, validation in the pipeline with FluentValidation.
- **Event-driven**: services exchange integration events through Kafka.
- **Outbox Pattern**: outgoing events are first written to the database, then published to Kafka by a background processor (at-least-once guarantee).
- **Typed errors**: `ErrorOr<T>` for expected business-logic and validation errors.

## Technology stack

| Category | Technologies |
| -------- | ------------ |
| Platform | .NET 10, ASP.NET Core |
| Data | PostgreSQL 16, EF Core, Npgsql |
| Cache | Redis 7, Memory Cache |
| Messaging | Apache Kafka (Confluent) |
| Architectural patterns | Clean Architecture, CQRS (MediatR), Outbox |
| Validation and errors | FluentValidation, ErrorOr |
| Authentication | JWT (access + refresh), BCrypt |
| Resilience | Polly (retry, circuit breaker, timeout), `IHttpClientFactory` |
| Logging | Serilog (console, file, Seq) |
| API documentation | Swashbuckle (Swagger / OpenAPI) |
| Containerization | Docker, Docker Compose |
| Testing | xUnit, Testcontainers |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/)

## Quick start

From the repository root, a single command brings up both services and all infrastructure (PostgreSQL, Redis, Kafka):

```bash
docker compose --project-directory OrderService -f docker-compose.kafka.yml -f OrderService/docker-compose.yml -f PaymentService/docker-compose.yml up -d
```

Once started, the following are available:

| URL | Purpose |
| --- | ------- |
| http://localhost:8080 | Kafka UI |
| http://localhost:5001/swagger | OrderService Swagger |
| http://localhost:5002/swagger | PaymentService Swagger |

Stop the stack:

```bash
docker compose --project-directory OrderService -f docker-compose.kafka.yml -f OrderService/docker-compose.yml -f PaymentService/docker-compose.yml down
```

## Ports and infrastructure

| Component | Port | Purpose |
| --------- | ---- | ------- |
| OrderService API | 5001 | HTTP / Swagger |
| PaymentService API | 5002 | HTTP / Swagger |
| Kafka UI | 8080 | Monitoring topics and messages |
| PostgreSQL (order) | 5432 | OrderService database |
| PostgreSQL (payment) | 5433 | PaymentService database |
| Redis | 6379 | OrderService distributed cache |
| Kafka | 29092 (localhost) / 9092 (internal) | Message broker |

## Configuration

- Default values are stored in each service's `appsettings.json` and contain no secrets.
- Sensitive data (connection strings, JWT key) is supplied via environment variables or `appsettings.Development.json`.
- The section separator in environment variables is a double underscore (`__`), for example `ConnectionStrings__Postgres`.
- Each service has its own `Dockerfile` (multi-stage build) and can be deployed independently.

## Testing

Tests are run from the repository root:

```bash
dotnet restore
dotnet build --configuration Debug
```

Test projects are located in the `tests/` directories of the respective solutions and use Testcontainers to bring up dependencies (PostgreSQL). Naming: `*.UnitTests.csproj` and `*.IntegrationTests.csproj`.

```bash
find . -name "*.IntegrationTests.csproj" -exec dotnet test {} \;
```

## CI

The workflow `.github/workflows/master_pull_req_actions.yml` runs on push and pull request to the `master` branch:

1. install the .NET 10 SDK;
2. `dotnet restore` and `dotnet build` the entire repository;
3. run unit and integration tests;
4. upload the results (`TestResults/`) as an artifact.

## Documentation

- [OrderService — architecture, API, configuration](OrderService/README.md)
- [PaymentService — architecture, API, configuration](PaymentService/README.md)

## General conventions

- The .NET version, `Nullable`, `ImplicitUsings`, and analyzers are configured centrally in `Directory.Build.props`; nullable warnings are treated as errors.
- Versions of all NuGet packages are pinned in `Directory.Packages.props` (central package management) — individual `.csproj` files do not specify `Version`.
