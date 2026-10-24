# Garbati.ProductApi

GARBATI's product & pricing service — the "Pricing Desk" in the platform's own floor-plan: it decides
what rules, fees and limits apply to a product; it never executes anything. `LedgerApi` posts money,
`Garbati.IdentityApi` decides who's allowed to act, `Garbati.OrchestratorApi` carries a request between
them — this service answers "what are this product's rules," nothing more. Built with ASP.NET Core 8,
EF Core and PostgreSQL, matching the rest of the platform.

The design, including every trade-off, is in [docs/DESIGN.md](docs/DESIGN.md) — start there for the
*why*, especially Section 0: this service's fee and Sharia-related rules are deliberately left empty
pending a qualified Islamic finance scholar's sign-off (`research/sharia-product-rules.md` in the
`garbati-platform` hub repo). The catalog/binding/limits mechanics are real and buildable now; actual fee
amounts and profit-sharing terms are not invented here.

## What this build covers

This is ProductApi's **first** build task (`garbati-platform` T-045) and its entire v1 (minimal) scope
in one go. It covers:

- `Product` / `ProductLimitPolicy` / `AccountProductBinding` entities, built and seeded (the limit
  policy's two placeholder rows) for real.
- `FeeRule` / `ProfitSharingTerms` tables, created by migration and **never seeded** — see
  [docs/DESIGN.md](docs/DESIGN.md) Section 0 and Appendix A for the full reasoning. No endpoint or
  application code anywhere reads, writes or computes against `ProfitSharingTerms`.
- All 5 endpoints in `docs/DESIGN.md` Section 3: create/get a product, create/get an account binding, a
  fee quote that always answers `NoFeeConfigured` for every real caller today.
- API-key authentication, three scopes (`product.catalog.read`, `product.catalog.write`,
  `product.pricing.read`), audit logging, health checks.

This service makes **no outbound calls of its own** — like `StaffIdentityApi`, it is the opposite shape
from `OrchestratorApi`: nothing to mock in tests, nothing to configure as a downstream dependency. Its
one caller, `OrchestratorApi`, is not yet wired up to call it — that is a separate, future task.

## The empty-tables rule — read this before touching `FeeRule` or `ProfitSharingTerms`

`FeeRule` and `ProfitSharingTerms` exist as real database tables (via `Migrations/*InitialCreate*`) but
are **never seeded by any migration, any test, or any application code path**. `POST /v1/fee-quotes`
looks up a currently-effective `FeeRule` row and, finding none, returns an explicit
`{"feeResult":"NoFeeConfigured"}` — never a 404, never a 0-amount success, never an exception. This is a
deliberate, owner-decided boundary (no Sharia-sensitive fee or profit-sharing content may be invented
without a qualified scholar's sign-off that doesn't exist yet), not an oversight. See
[docs/DESIGN.md](docs/DESIGN.md) Section 0.

## Core pieces

- **Product** — `ProductCode`, `ProductName`, `JurisdictionCode`, `CurrencyCode`, `ProductType`
  (`Savings`/`Current`), effective-dated. A partial unique index enforces "at most one currently-effective
  row per `ProductCode`."
- **ProductLimitPolicy** — `MinimumBalance`, `AllowedOperations` (a configurable `jsonb` string list,
  copying `Garbati.IdentityApi`'s `KycTierPolicy.RequiredEvidence` pattern exactly). Seeded with two
  explicit placeholder rows (`NG-SAV-STD`, `NG-CUR-STD`) — not real, confirmed Nigerian figures.
- **AccountProductBinding** — links a `LedgerApi` account number to a `ProductCode`. This service never
  calls `LedgerApi` to check the account exists; `OrchestratorApi` already did that.
- **FeeRule** / **ProfitSharingTerms** — shape only, deliberately empty (see above).
- **ProductAuditLog** — one row per `Product` creation and per `AccountProductBinding` creation, staged
  on the same `DbContext` as the change it describes.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (pinned by `global.json`)
- [Docker](https://www.docker.com/) (for the local database and for the tests)

## Getting started

**1. Start PostgreSQL.** The Development connection string expects it on port `5438` (LedgerApi `5433`,
IdentityApi `5434`, OrchestratorApi `5435`, StaffIdentityApi `5436`, OpsApi `5437`):

```bash
docker run -d --name productapi-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=productapi -p 5438:5432 postgres:16-alpine
```

**2. Apply the migrations.** `dotnet-ef` is pinned in the repo's tool manifest:

```bash
dotnet tool restore
dotnet ef database update --project src/Garbati.ProductApi
```

This also runs the one-time `ProductLimitPolicy` placeholder seed.

**3. Run the API:**

```bash
dotnet run --project src/Garbati.ProductApi --launch-profile http
```

Swagger UI is at the printed `http://localhost:{port}/swagger` (click **Authorize** and paste
`dev-ops-api-key` — the plaintext behind the dev client configured in `appsettings.Development.json`).

## Running the tests

```bash
dotnet test
```

The whole suite runs against a real PostgreSQL container started by
[Testcontainers](https://dotnet.testcontainers.org/) (Docker must be running) and the real app in memory
(`WebApplicationFactory`) — no mocks, since this service calls nothing downstream.

## Endpoints

All under `/v1/` (`docs/DESIGN.md` Section 3):

| Method | Route | Scope | Purpose |
|---|---|---|---|
| `POST` | `/v1/products` | `product.catalog.write` | Create a product catalog entry. `409` if the code already has a currently-effective row |
| `GET` | `/v1/products/{productCode}` | `product.catalog.read` | Fetch a product plus its currently-effective limit policy fields (`null` if none exists yet) |
| `POST` | `/v1/account-bindings` | `product.catalog.write` | Bind an account to a product (`OrchestratorApi`'s account-opening step 5). Requires an idempotency key |
| `GET` | `/v1/account-bindings/{accountNumber}` | `product.catalog.read` | Which product (if any) an account is bound to |
| `POST` | `/v1/fee-quotes` | `product.pricing.read` | Given a product + operation type + amount, return the applicable fee — always `NoFeeConfigured` today |

### Status codes

| Status | Meaning |
|---|---|
| `200` | Success |
| `400` | Request failed validation |
| `401` | Missing/unknown API key |
| `403` | The API key lacks the endpoint's scope |
| `404` | Product, or account binding, not found |
| `409` | Idempotency key reused with a different request body, a currently-effective product code already exists, or the account is already bound to a different product |
| `500` | Unexpected error |

### Idempotency

`POST /v1/account-bindings` requires a client-supplied `idempotencyKey` (1-128 characters: letters,
digits, `-`, `_`, `.` or `:`), scoped to the calling API client. A retry with the same key and the same
other fields returns the same binding; the same key with different fields gets **409**.
`POST /v1/products` deliberately has no idempotency key — see `docs/DESIGN.md` Appendix A item 1 for why.

## Authentication

Same pattern as every sibling service (`ARCHITECTURE.md` Section 5): every request needs an `X-Api-Key`
header, and each key is granted **scopes**:

| Scope | Covers |
|---|---|
| `product.catalog.read` | `GET /v1/products/{productCode}`, `GET /v1/account-bindings/{accountNumber}` |
| `product.catalog.write` | `POST /v1/products`, `POST /v1/account-bindings` |
| `product.pricing.read` | `POST /v1/fee-quotes` |

A missing or unknown key gets **401**; a valid key without the endpoint's scope gets **403**.

### Development keys

`appsettings.Development.json` configures one local-only client. Never use this outside your machine.

| Client | Plaintext key (dev only) | Scopes |
|---|---|---|
| `dev-ops-api` | `dev-ops-api-key` | `product.catalog.read`, `product.catalog.write`, `product.pricing.read` |

### Issuing a key

Only the SHA-256 hash of a key is configured, never the key itself:

```bash
KEY="pak_$(openssl rand -base64 32 | tr '+/' '-_' | tr -d '=')"; echo "$KEY"; printf %s "$KEY" | openssl dgst -sha256 -binary | base64
```

## Logging and audit

Every request gets a correlation id (`X-Correlation-ID`). Audit rows (`product_audit_logs`) are written
in the same database transaction as the change they describe — one row per `Product` creation and per
`AccountProductBinding` creation. No `GET` endpoint and no fee-quote lookup is audited; neither changes
anything.

This is deliberately all GARBATI ships today — no metrics/tracing/alerting stack. See
`garbati-platform`'s `OBSERVABILITY.md` for why, and what to reach for if you need more.

## Deployment

The `Dockerfile` builds two images:

```bash
docker build -t productapi .                                   # the API
docker build --target migrator -t productapi-migrator .        # applies pending migrations, then exits
```

GitHub Actions (`.github/workflows/ci.yml`) runs the tests and builds both images on every pull request
and push to `main`. There is deliberately **no `deploy` job yet** — this service isn't being wired to
Azure in this task, matching how every sibling service started.

## Project layout

```
src/Garbati.ProductApi/
  Controllers/      ProductsController, AccountBindingsController, FeeQuotesController
  Services/         ProductService, AccountProductBindingService, FeeQuoteService
  Entities/         Product, ProductLimitPolicy, AccountProductBinding, FeeRule, ProfitSharingTerms, ProductAuditLog
  Data/             ProductDbContext and model configuration
  Migrations/       EF Core migrations, including the ProductLimitPolicy placeholder seed
  Validation/       Request validators, IdempotencyKeyRule, ProductTypeRule
  Middleware/       Correlation id, exception-to-status mapping
  Auditing/         IProductAuditLogger (stages rows on the request's DbContext)
  Authorization/    API-key authentication and scopes
  Contracts/        Request/response records
  Exceptions/
tests/Garbati.ProductApi.Tests/
  Controllers/      End-to-end tests over the real app + Testcontainers Postgres
  Authorization/, Configuration/, Data/, Fixtures/, Validation/
docs/DESIGN.md      Design document, decisions log, and Appendix A (T-045 build notes)
```

## Out of scope

Everything `docs/DESIGN.md` Section 0/1.2 defers: any real fee amount, tax rule, or profit-sharing ratio;
wiring `OrchestratorApi` to actually call this service; deploying to Azure; any endpoint or logic touching
`ProfitSharingTerms` at all.
