# Payments Batch Processing Service

A starter implementation of a payment batch processing service in C#/.NET using a modular monolith structure, SQLite for local persistence, Docker Compose for one-command startup, and a clear Azure deployment path.

## Design

See the full system design and architecture:
- [Design Document](docs/design.md)

## Features
- Create batches of payments and submit them for asynchronous, per-payment processing
- Submit batch for asynchronous processing
- Retry transient failures
- Track batch and payment statuses
- Record audit history for payment state transitions
- Idempotent batch creation with `X-Request-Id`

## Stack
- .NET 10
- ASP.NET Core Web API
- Worker Service
- EF Core with SQLite
- Docker Compose
- GitHub Actions
- Postman

## Repository layout
- `src/Payments.Api` - HTTP API
- `src/Payments.Application` - contracts and service abstractions
- `src/Payments.Domain` - entities and state rules
- `src/Payments.Infrastructure` - EF Core, fake bank adapter, processor, DI
- `src/Payments.Worker` - background worker host
- `tests` - starter tests
- `docs/design.md` - design documentation
- `postman` - collection and environment

## Assumptions
- Authentication is out of scope for this exercise.
- Bank integration is simulated with a fake adapter.
- SQLite is only for local/demo use.
- This repo targets .NET 10. 

## Run locally
```bash
docker compose up --build
```

API:
- `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

## Stop the app
```bash
docker compose down
```

## Reset the database
```bash
docker compose down -v
```

## Build locally without Docker
Requires a .NET 10 SDK.

```bash
dotnet restore payments-batch.sln
dotnet build payments-batch.sln
dotnet test payments-batch.sln
```

## Main endpoints
- `POST /api/batches`
- `POST /api/batches/{batchId}/submit`
- `GET /api/batches/{batchId}`
- `GET /api/batches/{batchId}/payments`
- `GET /api/payments/{paymentId}`

## Example request
`POST /api/batches`

Header:
- `X-Request-Id: 6f1f7a2d-1d90-4fc3-a5d0-c2f9d11b17f4`

Body:
```json
{
  "clientBatchReference": "APR-2026-001",
  "payments": [
    {
      "clientPaymentReference": "PAY-001",
      "currency": "USD",
      "amount": 100.25,
      "beneficiaryName": "Acme Ltd",
      "destinationAccount": "US123456789"
    }
  ]
}
```

## Postman
Import:
- `postman/Payments Batch Processing.postman_collection.json`
- `postman/local.postman_environment.json`

## GitHub Actions
- `ci.yml` restores, builds, and tests
- `deploy-azure-container-apps.yml` builds container images, pushes to ACR, and updates Azure Container Apps

## Azure path
For production, replace SQLite with Azure SQL and the worker polling loop with Azure Service Bus driven processing. Add Application Insights, Key Vault, Entra ID, and API Management.

---

## Tests

The solution includes three focused test layers:

- Domain tests – business rules and state transitions  
- Integration tests cover:
  - idempotent batch creation
  - create batch returns created
- Load tests (NBomber) – concurrent create/submit validation  

### Load testing (NBomber)

Run:

```bash
cd tests/Payments.LoadTests
dotnet run
```

Purpose:
- validate correctness under concurrency  
- verify idempotency and worker behavior  

Notes:
- not a performance benchmark  
- results are based on local SQLite environment  

---

## Observability

Observability is kept intentionally lightweight in the local solution:
- liveness and readiness health endpoints
- OpenTelemetry tracing and metrics
- reduced EF Core SQL logging verbosity

For production, telemetry would be exported to Azure Application Insights.