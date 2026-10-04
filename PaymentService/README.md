# PaymentService

A microservice that processes payments for orders in an e-commerce system. It consumes `payment.requested` integration events from Kafka, charges the amount through a pluggable payment gateway, and publishes the result back as `payment.succeeded` / `payment.failed`. Built with **ASP.NET Core 10** following **Clean Architecture** and **CQRS** via MediatR.

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Domain Overview](#domain-overview)
- [API Endpoints](#api-endpoints)
- [Messaging](#messaging)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Infrastructure Dependencies](#infrastructure-dependencies)
- [Database Indexes](#database-indexes)
- [Key Design Decisions](#key-design-decisions)
- [Background Services](#background-services)
- [Resilience](#resilience)
- [Logging](#logging)
- [Swagger](#swagger)

---

## Architecture

The solution follows **Clean Architecture** with four layers, where dependencies point inward:

| Layer            | Project                          | Responsibility                                                                 |
| ---------------- | -------------------------------- | ------------------------------------------------------------------------------ |
| **Domain**       | `PaymentService.Domain`          | Entities, enums, domain events, value types, gateway and repository interfaces. No external dependencies. |
| **Application**  | `PaymentService.Application`     | CQRS queries/handlers, DTOs, validators, integration events, ports (interfaces), pipeline behaviours. Orchestrates use cases. |
| **Infrastructure** | `PaymentService.Infrastructure` | EF Core + PostgreSQL, Kafka producer/consumer, outbox, gateway implementations, Polly resilience, background services. |
| **Presentation** | `PaymentService.Presentation`    | ASP.NET Core Web API, controllers, JWT authentication, Swagger, global error handling. |

## Tech Stack

- **.NET 10** / ASP.NET Core
- **PostgreSQL 16** — primary database (via EF Core + Npgsql)
- **Apache Kafka** — asynchronous messaging (inbox + outbox patterns)
- **MediatR** — request dispatching and in-process domain events
- **FluentValidation** — validation pipeline
- **ErrorOr** — typed result objects instead of exceptions for expected failures
- **JWT Bearer** — validates access tokens issued by OrderService
- **Polly** — HTTP resilience (retry, circuit breaker, timeout)
- **Serilog** — structured logging
- **Swashbuckle** — Swagger/OpenAPI
- **Docker** — containerized deployment

## Domain Overview

A payment moves through a small lifecycle:

```
Pending ──→ Processing ──→ Succeeded ──→ Refunded
                      └──→ Failed
```

1. **Pending** — the payment is registered from a `payment.requested` event.
2. **Processing** — the payment was handed to a gateway.
3. **Succeeded / Failed** — the gateway returned a terminal result. The entity records the external id or the failure reason and raises a domain event.
4. **Refunded** — a succeeded payment was refunded.

Only one payment may exist per order; the `Payments.OrderId` column has a unique index.

## API Endpoints

| Method | Endpoint                          | Auth          | Description                              |
| ------ | --------------------------------- | ------------- | ---------------------------------------- |
| GET    | `/api/payments/order/{orderId}`   | Authenticated | Get the payment for an order (owner only) |

## Messaging

| Topic               | Direction | Payload type                          | Triggered when                     |
| ------------------- | --------- | ------------------------------------- | ---------------------------------- |
| `payment.requested` | Consumed  | `PaymentRequestedIntegrationEvent`    | OrderService starts a payment      |
| `payment.succeeded` | Produced  | `PaymentSucceededIntegrationEvent`    | A gateway accepts the payment      |
| `payment.failed`    | Produced  | `PaymentFailedIntegrationEvent`       | A gateway declines the payment     |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for PostgreSQL and Kafka)

## Getting Started

The `docker-compose.yml` in the `PaymentService` directory starts PostgreSQL and the application. Kafka is provided by the shared `../docker-compose.kafka.yml` file.

From the repository root:

```bash
docker compose --project-directory OrderService -f docker-compose.kafka.yml -f PaymentService/docker-compose.yml up -d
```

This will:

- Start **PostgreSQL 16** on `localhost:5433` (database: `paymentservice`)
- Start **Kafka** (shared, from `docker-compose.kafka.yml`)
- Build and start **PaymentService** on `http://localhost:5002`

The application is available at `http://localhost:5002` with Swagger UI at `/swagger`.

## Configuration

### Environment Variables

All configuration values can be overridden via environment variables using double underscores (`__`) as section separators.

| Variable                                        | Description                                        | Default (Development)                                                     |
| ----------------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`                        | Runtime environment (`Development`, `Production`)  | `Development`                                                             |
| `ConnectionStrings__Postgres`                   | PostgreSQL connection string                       | `Host=localhost;Port=5432;Database=paymentservice;Username=postgres;Password=postgres` |
| `Jwt__SecretKey`                                | HMAC-SHA256 signing key (must match OrderService)  | `super-secret-key-for-development-at-least-32-chars!!`                    |
| `Jwt__Issuer`                                   | Expected JWT issuer                                | `OrderService`                                                            |
| `Jwt__Audience`                                 | Expected JWT audience                              | `OrderService`                                                            |
| `Kafka__BootstrapServers`                       | Kafka broker address                               | `localhost:9092`                                                          |
| `Kafka__GroupId`                                | Kafka consumer group id                            | `payment-service`                                                         |
| `Outbox__MaxRetryCount`                         | Max Kafka publish attempts per outbox message      | `5`                                                                       |
| `DomainEventRecovery__RecoveryThresholdSeconds` | Age after which pending domain events are redelivered | `20`                                                                   |
| `DomainEventRecovery__PollingIntervalSeconds`   | Delay between recovery scans                       | `10`                                                                      |
| `Resilience__RetryCount`                        | HTTP retry attempts                                | `3`                                                                       |
| `Resilience__CircuitBreakerFailureThreshold`    | Failures before the circuit breaker opens          | `5`                                                                       |
| `Resilience__CircuitBreakerBreakDurationSeconds`| Seconds the circuit stays open                     | `30`                                                                      |
| `Resilience__TimeoutSeconds`                    | Per-request HTTP timeout in seconds                | `10`                                                                      |
| `Serilog__*`                                    | Standard Serilog overrides                         | `Information`                                                             |

**Production warning:** never use the development secret key in production. Supply all secrets through environment variables or a secrets manager.

### appsettings.json Sections

| Section               | Keys                                                                 | Bound to                      |
| --------------------- | -------------------------------------------------------------------- | ----------------------------- |
| `ConnectionStrings`   | `Postgres`                                                           | —                             |
| `Jwt`                 | `SecretKey`, `Issuer`, `Audience`                                    | `JwtOptions`                  |
| `Kafka`               | `BootstrapServers`, `GroupId`                                        | `KafkaOptions`                |
| `Outbox`              | `MaxRetryCount`                                                      | `OutboxOptions`               |
| `DomainEventRecovery` | `RecoveryThresholdSeconds`, `PollingIntervalSeconds`                 | `DomainEventRecoveryOptions`  |
| `Resilience`          | `RetryCount`, `CircuitBreakerFailureThreshold`, `CircuitBreakerBreakDurationSeconds`, `TimeoutSeconds` | `ResilienceOptions` |
| `Serilog`             | Standard Serilog configuration                                       | Serilog                       |

## Infrastructure Dependencies

| Service      | Port  | Purpose                     | Required |
| ------------ | ----- | --------------------------- | -------- |
| PostgreSQL   | 5433  | Primary data store          | Yes      |
| Kafka        | 29092 | Event bus (inbox / outbox)  | Yes      |

## Database Indexes

| Table               | Columns                    | Unique | Rationale                                                                 |
| ------------------- | -------------------------- | ------ | ------------------------------------------------------------------------- |
| `Payments`          | `OrderId`                  | Yes    | One payment per order; fast lookup by order.                              |
| `InboxMessages`     | `MessageId`                | Yes    | Idempotent Kafka consumption — the unique key rejects duplicate messages. |
| `OutboxMessages`    | `(ProcessedAt, CreatedAt)` | No     | `OutboxProcessor` polls pending rows ordered by creation.                 |
| `DomainEventEntries`| `(ProcessedAt, CreatedAt)` | No     | `DomainEventRecoveryService` scans pending domain events.                 |

## Key Design Decisions

### Domain Events

Domain events are in-process notifications raised by aggregates (`PaymentCreatedDomainEvent`, `PaymentSucceededDomainEvent`, `PaymentFailedDomainEvent`, `PaymentRefundedDomainEvent`). They implement `IDomainEvent : INotification` and are collected through `AggregateRoot.DequeueDomainEvents()`.

- The unit of work persists each event to `DomainEventEntries` in the same transaction as the business change, then dispatches it to MediatR handlers after the save succeeds. Handlers require no manual registration — `AddMediatR` scans the Application assembly.
- Pending entries that were never marked processed are redelivered by `DomainEventRecoveryService`.

### Outbox Pattern

Outgoing integration events are written to the `OutboxMessages` table within the same transaction as the payment state change. `OutboxProcessor` polls the table and publishes unprocessed messages to Kafka. This guarantees **at-least-once** delivery: events stay durable in PostgreSQL and are retried if Kafka is unavailable.

### Inbox / Idempotency

The consumer records every message in the `InboxMessages` table keyed by the Kafka message key (or a topic/partition/offset fallback). Duplicate deliveries are detected and skipped, so processing a payment request twice has no additional effect.

### Payment Gateway Factory

`IPaymentGateway` abstracts an external provider. `PaymentGatewayFactory` resolves the implementation registered for a currency/method pair. Adding a new provider only requires a new `IPaymentGateway` implementation and its DI registration — the factory is unchanged (OCP). The shipped `MockPaymentGateway` simulates a provider in development.

### Scoping

Application depends only on abstractions it declares (ports such as `IUnitOfWork`, `IMessageProducer`) and on the Domain layer. Infrastructure references Application (never the other way around) and provides the implementations, which are wired up in `PaymentService.Infrastructure/DependencyInjection.cs`.

## Background Services

| Service                      | Interval   | Purpose                                                            |
| ---------------------------- | ---------- | ------------------------------------------------------------------ |
| `KafkaConsumerService`       | continuous | Consumes `payment.requested`, deduplicates via the inbox, processes the payment |
| `OutboxProcessor`            | 5 sec      | Publishes pending outbox messages to Kafka                         |
| `DomainEventRecoveryService` | 10 sec     | Redelivers domain events older than the recovery threshold         |

## Resilience

The named HTTP client `HttpClientNames.Resilient` is configured with **Polly** policies (values come from the `Resilience` section):

- **Retry** — `Resilience:RetryCount` retries with exponential backoff on transient HTTP errors.
- **Circuit Breaker** — opens after `Resilience:CircuitBreakerFailureThreshold` consecutive failures and resets after `Resilience:CircuitBreakerBreakDurationSeconds`.
- **Timeout** — `Resilience:TimeoutSeconds` per HTTP call.

## Logging

The service uses **Serilog**. Configuration is read from the `Serilog` section, the `Service` property is enriched with `PaymentService`, and logs are written to the **console**. Request logging is enabled via `UseSerilogRequestLogging()`.

## Swagger

Swagger UI is available at `/swagger` in the **Development** environment and includes a JWT Bearer authorization button. Paste an access token issued by OrderService (without the `Bearer` prefix) to authenticate requests.
