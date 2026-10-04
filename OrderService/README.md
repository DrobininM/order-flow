# OrderService

A microservice for managing orders, products, and authentication in an e-commerce system. Built with **ASP.NET Core 10** following **Clean Architecture** principles and **CQRS** pattern via MediatR.

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Domain Overview](#domain-overview)
- [API Endpoints](#api-endpoints)
- [Authentication & Authorization](#authentication--authorization)
  - [Rate Limiting](#rate-limiting)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Running with Docker Compose](#running-with-docker-compose)
  - [Running Locally](#running-locally)
- [Configuration](#configuration)
  - [Environment Variables](#environment-variables)
  - [appsettings.json Sections](#appsettingsjson-sections)
- [Infrastructure Dependencies](#infrastructure-dependencies)
- [Database Indexes](#database-indexes)
- [Key Design Decisions](#key-design-decisions)
  - [Domain Events](#domain-events)
  - [Outbox Pattern](#outbox-pattern)
  - [Reservation Flow](#reservation-flow)
  - [Refresh Token Rotation](#refresh-token-rotation)
- [Background Services](#background-services)
- [Resilience](#resilience)
- [Logging](#logging)
- [Swagger](#swagger)

---

## Architecture

The solution follows **Clean Architecture** with four layers, where dependencies point inward:

| Layer            | Project                          | Responsibility                                                                 |
| ---------------- | -------------------------------- | ------------------------------------------------------------------------------ |
| **Domain**       | `OrderService.Domain`            | Entities, value objects, enums, domain events, repository interfaces. No external dependencies. |
| **Application**  | `OrderService.Application`       | CQRS commands/queries/handlers, DTOs, validators, integration events, ports (interfaces). Orchestrates use cases. |
| **Infrastructure** | `OrderService.Infrastructure`  | EF Core + PostgreSQL, Redis caching, Kafka messaging, JWT tokens, BCrypt hashing, Polly resilience, background services. |
| **Presentation** | `OrderService.Presentation`      | ASP.NET Core Web API, controllers, JWT authentication middleware, Swagger, global error handling. |

## Tech Stack

- **.NET 10** / ASP.NET Core
- **PostgreSQL 16** — primary database (via EF Core + Npgsql)
- **Redis 7** — distributed caching
- **Apache Kafka** — asynchronous messaging (outbox pattern)
- **MediatR** — CQRS command/query dispatching
- **FluentValidation** — request validation pipeline
- **ErrorOr** — typed result objects instead of exceptions for flow control
- **BCrypt.Net-Next** — password hashing
- **JWT Bearer** — authentication (access + refresh tokens)
- **Polly** — HTTP resilience (retry, circuit breaker)
- **Serilog** — structured logging (console by default; file/Seq via configuration)
- **Swashbuckle** — Swagger/OpenAPI
- **Docker** — containerized deployment

## Domain Overview

### Order Lifecycle

```
Draft ──→ Reserved ──→ Paid
  │           │
  └── Cancelled ←──┘
```

1. **Draft** — User creates an empty order, adds items. No stock is consumed.
2. **Reserved** — User initiates payment. Products are reserved (stock not yet deducted, but `ReservedQuantity` incremented). A `payment.requested` integration event is published to Kafka. Reservation expires after 10 minutes.
3. **Paid** — External payment succeeds. Stock is committed (deducted). A `PaymentSucceededDomainEvent` is raised.
4. **Cancelled** — Order is cancelled either manually or automatically (reservation timeout). Reserved quantities are released back.

### Product Reservation

- `StockQuantity` — total physical stock
- `ReservedQuantity` — quantity locked by pending (Reserved) orders
- `AvailableQuantity` = `StockQuantity - ReservedQuantity` — what new orders can claim

### User Roles

| Role       | Value | Permissions                                      |
| ---------- | ----- | ------------------------------------------------ |
| Customer   | 1     | Create/view own orders, view products            |
| Admin      | 2     | All customer permissions + full product CRUD + view all orders |

## API Endpoints

### Authentication

| Method | Endpoint            | Auth     | Description                                  |
| ------ | ------------------- | -------- | -------------------------------------------- |
| POST   | `/api/auth/register`| No       | Register a new user account                  |
| POST   | `/api/auth/login`   | No       | Authenticate and receive access + refresh tokens |
| POST   | `/api/auth/refresh` | No       | Rotate refresh token, get a new token pair   |

### Products

| Method | Endpoint               | Auth          | Description                     |
| ------ | ---------------------- | ------------- | ------------------------------- |
| GET    | `/api/products`        | No            | Paged list of products          |
| GET    | `/api/products/{id}`   | No            | Get product by ID               |
| POST   | `/api/products`        | Admin         | Create a new product            |
| PUT    | `/api/products/{id}`   | Admin         | Update an existing product      |
| DELETE | `/api/products/{id}`   | Admin         | Soft-delete (mark unavailable)  |

### Orders

| Method | Endpoint                    | Auth          | Description                                          |
| ------ | --------------------------- | ------------- | ---------------------------------------------------- |
| POST   | `/api/orders`               | Authenticated | Create a new draft order                             |
| GET    | `/api/orders`               | Authenticated | Paged list of orders (admins see all, users see own) |
| GET    | `/api/orders/{id}`          | Authenticated | Get order by ID (owner or admin)                     |
| POST   | `/api/orders/{id}/items`    | Authenticated | Add an item to a draft order                         |
| POST   | `/api/orders/{id}/pay`      | Authenticated | Initiate payment (reserve products, publish event)   |
| POST   | `/api/orders/{id}/cancel`   | Authenticated | Cancel a draft or reserved order                     |

### Query Parameters

- **GET /api/products** and **GET /api/orders** support pagination: `?skip=0&take=20`

## Authentication & Authorization

Authentication uses **JWT Bearer** access tokens. For any endpoint marked *Authenticated* or *Admin*, send the token in the request header:

```
Authorization: Bearer <access_token>
```

- Obtain a token pair (`accessToken` + `refreshToken`) from `POST /api/auth/login` or `POST /api/auth/register`.
- Access tokens expire after `Jwt:AccessTokenExpirationMinutes` (default 15 min). When they do, call `POST /api/auth/refresh` with the refresh token to get a new pair.
- *Authenticated* endpoints require the `[Authorize]` attribute; *Admin* endpoints additionally require the `Admin` role (`[Authorize(Roles = "Admin")]`).
- A missing or invalid/expired token returns `401 Unauthorized`; a valid token lacking the required role returns `403 Forbidden`.
- In Swagger UI use the **Authorize** button and paste the token **without** the `Bearer` prefix (see [Swagger](#swagger)).

### Rate Limiting

Fixed-window rate limits (per client IP for anonymous endpoints, per user id once authenticated). Rejected requests return `429 Too Many Requests` with a `RateLimit.Exceeded` body.

| Policy          | Endpoints                              | Limit   |
| --------------- | -------------------------------------- | ------- |
| `auth`          | register / login / refresh             | 10 / min |
| `payment`       | `POST /api/orders/{id}/pay`            | 5 / min  |
| `orders`        | create order / add item / cancel       | 30 / min |
| `products-read` | `GET /api/products`, `GET /api/products/{id}` | 120 / min |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for running PostgreSQL, Redis, and Kafka)
- Optional: [Seq](https://datalust.co/seq) (for structured log aggregation)

## Getting Started

### Running with Docker Compose

The `docker-compose.yml` file in the `OrderService` directory starts PostgreSQL, Redis, and the OrderService application. For Kafka you'll need a separate instance or add it to the compose file.

```bash
# From the OrderService directory (where docker-compose.yml lives)
docker compose up -d
```

This will:
- Start **PostgreSQL 16** on `localhost:5432` (database: `orderservice`, user: `postgres`, password: `postgres`)
- Start **Redis 7** on `localhost:6379`
- Build and start the **OrderService** app on `http://localhost:5001`

The application will be available at `http://localhost:5001` with Swagger UI at `/swagger`.

## Configuration

### Environment Variables

All configuration values can be set via environment variables, which override `appsettings.json` values. The naming convention follows the .NET configuration pattern — use double underscores (`__`) or colons (`:`) as section separators.

| Variable                              | Description                                      | Default (Development)                                                   |
| ------------------------------------- | ------------------------------------------------ | ----------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`              | Runtime environment (`Development`, `Production`) | `Development`                                                          |
| `ConnectionStrings__Postgres`         | PostgreSQL connection string                     | `Host=localhost;Port=5432;Database=orderservice;Username=postgres;Password=postgres` |
| `ConnectionStrings__Redis`            | Redis connection string                          | `localhost:6379`                                                        |
| `Jwt__SecretKey`                      | HMAC-SHA256 signing key (min 32 characters)      | `super-secret-key-for-development-at-least-32-chars!!`                  |
| `Jwt__Issuer`                         | JWT issuer claim                                 | `OrderService`                                                          |
| `Jwt__Audience`                       | JWT audience claim                               | `OrderService`                                                          |
| `Jwt__AccessTokenExpirationMinutes`   | Access token lifetime in minutes                 | `15`                                                                    |
| `Jwt__RefreshTokenExpirationDays`     | Refresh token lifetime in days                   | `7`                                                                     |
| `Kafka__BootstrapServers`             | Kafka broker address                             | `localhost:9092`                                                        |
| `Kafka__GroupId`                      | Kafka consumer group id                          | `order-service`                                                         |
| `Email__BaseUrl`                      | Base URL of the HTTP email provider              | *(empty)*                                                               |
| `Orders__ReservationTimeoutMinutes`   | Minutes before a product reservation expires     | `10`                                                                    |
| `Outbox__MaxRetryCount`               | Max Kafka publish attempts per outbox message    | `5`                                                                     |
| `Resilience__RetryCount`              | HTTP retry attempts                              | `3`                                                                     |
| `Resilience__CircuitBreakerFailureThreshold` | Failures before the circuit breaker opens | `5`                                                                    |
| `Resilience__CircuitBreakerBreakDurationSeconds` | Seconds the circuit stays open        | `30`                                                                    |
| `Resilience__TimeoutSeconds`          | Per-request HTTP timeout in seconds              | `10`                                                                    |
| `DomainEventRecovery__RecoveryThresholdSeconds` | Age after which pending domain events are redelivered | `20`                                    |
| `DomainEventRecovery__PollingIntervalSeconds`   | Delay between recovery scans                          | `10`                                    |
| `Serilog__*`                          | Standard Serilog overrides (e.g. `Serilog__MinimumLevel__Default`) | `Information`                        |

**Production warning:** Never use the development secret key in production. Generate a strong key (at least 32 characters) and store it in a secrets manager or environment variable.

### appsettings.json Sections

`appsettings.json` defines the default shape of the configuration (`Postgres`, `Redis`, and `Jwt:SecretKey` are intentionally empty), while `appsettings.Development.json` supplies local values. Each section is bound to a strongly-typed options class:

| Section                 | Keys                                                                 | Bound to                    | Purpose                                                        |
| ----------------------- | -------------------------------------------------------------------- | --------------------------- | -------------------------------------------------------------- |
| `ConnectionStrings`     | `Postgres`, `Redis`                                                  | —                           | Database and cache connections. Both are **required** at startup. |
| `Jwt`                   | `SecretKey`, `Issuer`, `Audience`, `AccessTokenExpirationMinutes`, `RefreshTokenExpirationDays` | `JwtOptions` / `AuthOptions` | Token issuing and validation. `SecretKey` is required.         |
| `Kafka`                 | `BootstrapServers`, `GroupId`                                        | `KafkaOptions`              | Broker address (producer + consumer) and consumer group id.    |
| `Email`                 | `BaseUrl`                                                            | `EmailOptions`              | Base URL of the HTTP email provider.                           |
| `Orders`                | `ReservationTimeoutMinutes`                                          | `OrderOptions`              | Lifetime of a product reservation.                             |
| `Outbox`                | `MaxRetryCount`                                                      | `OutboxOptions`             | Max Kafka publish attempts before an outbox message is abandoned. |
| `DomainEventRecovery`   | `RecoveryThresholdSeconds`, `PollingIntervalSeconds`                 | `DomainEventRecoveryOptions`| Redelivery of pending domain events.                           |
| `Resilience`            | `RetryCount`, `CircuitBreakerFailureThreshold`, `CircuitBreakerBreakDurationSeconds`, `TimeoutSeconds` | `ResilienceOptions` | Polly HTTP policies (see [Resilience](#resilience)).           |
| `Serilog`               | Standard Serilog configuration (minimum levels and `WriteTo` sinks)  | Serilog                     | Logging sinks and levels (see [Logging](#logging)).            |

> Secrets (`ConnectionStrings:Postgres`, `ConnectionStrings:Redis`, `Jwt:SecretKey`) must be supplied via `appsettings.Development.json`, environment variables, or a secrets manager — never committed.

## Infrastructure Dependencies

| Service      | Port  | Purpose                          | Required |
| ------------ | ----- | -------------------------------- | -------- |
| PostgreSQL   | 5432  | Primary data store               | Yes      |
| Redis        | 6379  | Distributed cache                | Yes      |
| Kafka        | 9092  | Event bus (outbox delivery)      | Yes      |
| Seq (opt.)   | 5341  | Structured log aggregation       | No       |

## Database Indexes

Indexes are declared in the EF Core configurations under `OrderService.Infrastructure/Persistence/Configurations`. EF Core additionally creates indexes for foreign keys automatically.

| Table               | Columns                  | Unique | Rationale                                                                                   |
| ------------------- | ------------------------ | ------ | ------------------------------------------------------------------------------------------- |
| `Users`             | `Email`                  | Yes    | One account per email and fast lookup during registration/login.                            |
| `RefreshTokens`     | `Token`                  | Yes    | Tokens are resolved by value on every refresh; uniqueness also prevents collisions.         |
| `RefreshTokens`     | `UserId`                 | No     | FK index used when loading or revoking all tokens of a user (theft detection revokes the set). |
| `OutboxMessages`    | `(ProcessedAt, CreatedAt)` | No   | `OutboxProcessor` polls for unprocessed rows ordered by creation; the composite index keeps the scan cheap. |
| `DomainEventEntries`| `(ProcessedAt, CreatedAt)` | No   | Same access pattern for `DomainEventRecoveryService` scanning pending domain events.        |
| `InboxMessages`     | `MessageId`              | Yes    | Idempotent Kafka consumption — the unique key rejects duplicate deliveries of the same message. |
| `OrderItems`        | `OrderId`                | No     | FK index used to materialize an order's items.                                              |

## Key Design Decisions

### Domain Events

Domain events are in-process notifications that signal a business fact (e.g. `OrderPaidDomainEvent`, `PaymentFailedDomainEvent`). They live in `OrderService.Domain.DomainEvents` and implement `IDomainEvent : INotification` (MediatR).

- Aggregates raise them via `AggregateRoot.AddDomainEvent(...)`; they are dequeued when the unit of work saves.
- Handlers are ordinary MediatR `INotificationHandler<T>` implementations (`OrderReservedDomainEventHandler`, `OrderPaidDomainEventHandler`, `PaymentFailedDomainEventHandler`). **No manual registration is required** — `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` in `OrderService.Application.DependencyInjection` scans the Application assembly and wires up every handler automatically.
- Persistence follows the [Outbox Pattern](#outbox-pattern) below: the event is written to `DomainEventEntries` in the same transaction, then dispatched after commit; unprocessed entries are retried by `DomainEventRecoveryService`.

### Outbox Pattern

The service uses two distinct outboxes:

- **Domain events** are persisted to a `DomainEventEntries` table in the same database transaction as the business change, then dispatched to in-process MediatR handlers after the save succeeds. Each entry is marked as processed on success; `DomainEventRecoveryService` redelivers entries that remain unprocessed for longer than `DomainEventRecovery__RecoveryThresholdSeconds` (default 20 seconds).
- **Integration events** are persisted to an `OutboxMessages` table in the same transaction as the business operation. A background service (`OutboxProcessor`) polls this table every 5 seconds and publishes unprocessed messages to Kafka.

Both guarantee **at-least-once** delivery even if a handler or Kafka is temporarily unavailable — the event is durable in PostgreSQL and will be retried.

**Integration event → Kafka topic mapping** (`KafkaMessageRouter`):

| Event Type                              | Kafka Topic         | Direction | Triggered When                          |
| --------------------------------------- | ------------------- | --------- | --------------------------------------- |
| `OrderCreatedIntegrationEvent`          | `order.created`     | Produced  | A new draft order is created            |
| `OrderCancelledIntegrationEvent`        | `order.cancelled`   | Produced  | An order is cancelled                   |
| `PaymentRequestedIntegrationEvent`      | `payment.requested` | Produced  | Payment is initiated (order reserved)   |
| `PaymentSucceededIntegrationEvent`      | `payment.succeeded` | Consumed  | External payment succeeds               |
| `PaymentFailedIntegrationEvent`         | `payment.failed`    | Consumed  | External payment fails                  |

Outbound integration events are stored in `OutboxMessages` and routed to their topic by `KafkaMessageRouter`; an event type without a mapping throws `InvalidOperationException`. Inbound payment results are consumed by `KafkaPaymentConsumerService`, deduplicated through the `InboxMessages` table, and dispatched to `ProcessPaymentResultHandler`.

### Reservation Flow

1. User calls `POST /api/orders/{id}/pay`
2. A database transaction begins
3. Products are locked with `SELECT ... FOR UPDATE` to prevent race conditions
4. Each product's `CanReserve()` is checked; if any fails, the transaction rolls back
5. Stock is reserved (`ReservedQuantity` incremented)
6. Order status moves to `Reserved` with a 10-minute expiration
7. A `PaymentRequestedIntegrationEvent` outbox message is created
8. Transaction commits atomically

If the external payment succeeds or fails, a separate service publishes `PaymentSucceeded` or `PaymentFailed` integration events back. These are consumed by `KafkaPaymentConsumerService` and applied idempotently by `ProcessPaymentResultHandler`, which commits or releases the reservation.

### Refresh Token Rotation

- Login returns an **access token** (15 min) and a **refresh token** (7 days)
- Refresh endpoint accepts a valid refresh token and returns a **new token pair**
- The old refresh token is invalidated (rotation)
- If a revoked token is reused, **all** refresh tokens for that user are revoked — this is a theft detection mechanism

## Background Services

| Service                        | Interval   | Purpose                                                       |
| ------------------------------ | ---------- | ------------------------------------------------------------- |
| `KafkaPaymentConsumerService`  | continuous | Consumes `payment.succeeded` / `payment.failed`, deduplicates via the inbox, dispatches payment results |
| `OutboxProcessor`              | 5 sec      | Publishes pending outbox messages to Kafka                    |
| `ReservationTimeoutProcessor`  | 30 sec     | Cancels orders whose reservation has expired, releases stock  |
| `DomainEventRecoveryService`   | 10 sec     | Redelivers domain events older than the recovery threshold    |

## Resilience

The named HTTP client `HttpClientNames.Resilient` — currently used by `HttpEmailService` — is configured with **Polly** policies (all values come from the `Resilience` configuration section):

- **Retry**: `Resilience:RetryCount` retries (default 3) with exponential backoff (2s, 4s, 8s) on transient HTTP errors.
- **Circuit Breaker**: opens after `Resilience:CircuitBreakerFailureThreshold` consecutive failures (default 5) and resets after `Resilience:CircuitBreakerBreakDurationSeconds` (default 30 s).
- **Timeout**: `Resilience:TimeoutSeconds` per HTTP call (default 10 s).

## Logging

The service uses **Serilog**. Configuration is read from the `Serilog` section, the `Service` property is enriched with the value `OrderService`, and logs are always written to the **console**. Additional sinks (file, Seq) are only active if a `Serilog:WriteTo` array is added to configuration — the shipped `appsettings.json` defines minimum levels only.

Request logging is enabled via `UseSerilogRequestLogging()`.

## Swagger

Swagger UI is available at `/swagger` in the **Development** environment. It includes a JWT Bearer authorization button — paste your access token (without the `Bearer` prefix) to authenticate requests.

---