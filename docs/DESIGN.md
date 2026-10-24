# Garbati.ProductApi — Design Document (v1, minimal)

Picks up the "Pricing Desk" role named in `garbati-platform/ARCHITECTURE.md`'s service map since day
one (decision P-2, 16 Oct 2026) and in T-009 ("Product questions-only design skeleton — first Sharia
review point"), never previously built. This is the fourth service to exist, after `LedgerApi`,
`Garbati.IdentityApi` and `Garbati.OrchestratorApi` — read their own `docs/DESIGN.md` files for the shared
conventions (`garbati-platform/ARCHITECTURE.md` Section 5) this one follows exactly: API key + scopes
authentication, client-scoped idempotency keys, append-only audit trail, `X-Correlation-ID` propagation,
the same error-code table, `/v1/` routes, `/health/live` + `/health/ready`.

## 0. The scope decision this design is built on (owner, 23 Oct 2026)

`research/sharia-product-rules.md` (T-007) is explicit: it "does not decide anything," needs a qualified
Islamic finance scholar's sign-off before any of its content becomes a real product rule, and several
specific points (late-fee/ta'widh handling, the boundary between a legitimate service fee and disguised
interest) have genuine, unresolved disagreement between the sources it found. No scholar has been
appointed yet. Building real fee amounts or real profit-sharing terms into this service today would mean
inventing a rule nobody qualified has actually approved.

**So v1 is scoped in two halves, deliberately:**

1. **Built for real now** — the parts that are mechanical, not religious or legal judgment calls: the
   product catalog's shape, binding an account to a product, and the minimum-balance/allowed-operations
   limit mechanism (the exact numbers are still placeholders pending compliance confirmation, same as
   `Garbati.IdentityApi`'s `KycTierPolicy` seed data already is — a precedent this design deliberately
   follows, not invents fresh).
2. **Structurally present, deliberately empty** — fee/tax rules and profit-sharing terms get a real table
   shape and a real endpoint contract, so `OrchestratorApi` has something stable to integrate against
   later, but **no real rule is ever seeded**. Asking for a fee quote today returns an explicit
   "not yet configured" result, never an invented number. This is the same discipline T-044 used for
   `GovernmentId`/`SourceOfFunds` KYC evidence — present the shape, leave the content unenforced/unfilled
   until the thing it actually depends on exists.

This is not a workaround to ship something anyway — it's the honest shape of what can responsibly exist
before a scholar is appointed. **Nothing in this document should be read as a Sharia ruling or a finalized
fee policy.**

## 1. Purpose and scope

Owns the product catalog, which account is bound to which product, and the one place product-level rules
(minimum balance, allowed operations, fees, tax, limits) are decided — never executed. Per
`ARCHITECTURE.md` Section 4.4: "ProductApi computes the amount. The orchestrator posts it. ProductApi
never touches the ledger." This service has no door to `LedgerApi` and no door to `Garbati.IdentityApi` —
it only ever answers `OrchestratorApi`, the same "answers, never dials out" shape `LedgerApi` and
`Garbati.IdentityApi` already both have.

### 1.1 What this service deliberately does NOT own

- Money movement, balances, or anything `LedgerApi` already owns.
- Customer identity, KYC status/tier, or mandates — `Garbati.IdentityApi`'s job.
- The sequence of steps in a customer operation, or any decision id — `OrchestratorApi`'s job. This
  service only ever answers a single, stateless question per call; it holds no operation-in-progress
  state of its own.
- Real fee amounts or real Sharia product-rule content (see Section 0) — **explicitly out of scope for
  v1**, pending scholar and compliance sign-off.

### 1.2 Explicitly out of scope for v1

- Any real, numeric fee/tax/profit-sharing rule.
- Standing-order/recurring-payment product variants (`ScheduleApi`'s job once it exists).
- Financing products (Murabaha/Musharaka/Mudarabah contracts and schedules) — a separate "Financing
  services" tranche per the service map, not this catalog.
- A reconciliation job comparing posted fees against this service's rules (`ARCHITECTURE.md`'s own
  open-question note on this) — meaningless until real fee rules exist to reconcile against.

## 2. Entities

### 2.1 `Product`

The catalog entry. `ProductCode` (e.g. `NG-SAV-STD`), `ProductName`, `JurisdictionCode`, `CurrencyCode`,
`ProductType` (`Savings` | `Current` for v1 — a real, mechanical distinction, not a Sharia judgment call),
`EffectiveFrom`/`EffectiveTo` — same effective-dating convention `KycTierPolicy` already established, so
a product's terms can change over time without losing the history of what applied when.

### 2.2 `ProductLimitPolicy`

`ProductCode`, `MinimumBalance`, `AllowedOperations` (a configurable jsonb string list, not hardcoded in
application code — the exact same "configurable, not hardcoded" shape as `KycTierPolicy.RequiredEvidence`,
decision #11 in `Garbati.IdentityApi`). **Seeded only with explicit placeholder values**, marked in the
seed migration's own comment exactly the way `KycTierPolicy`'s placeholder rows already are — real Nigerian
minimum-balance figures still need compliance confirmation (`research/nigerian-regulatory-requirements.md`
already flags this general category of number as unverified against a primary CBN circular).

### 2.3 `AccountProductBinding`

Links a `LedgerApi` account (by its account number — this service never calls `LedgerApi` to validate it
exists; `OrchestratorApi` is the one that confirmed that already, during the step before this one) to a
`ProductCode`, with `BoundAt`. This is what `ARCHITECTURE.md`'s own account-opening sequence (Section 10)
already calls step 5, "Bind product version" — this service is what that step has been waiting on.

### 2.4 `FeeRule` — shape only, deliberately empty

`ProductCode`, `OperationType`, `FeeType` (`Flat` | `Percentage` | `None` — the shape AAOIFI-style
service-fee-vs-interest distinction will eventually need, per `research/sharia-product-rules.md` Q3, not
a ruling on which type is permitted for which case), `Amount`, `EffectiveFrom`/`EffectiveTo`. **No row is
ever seeded for v1.** A fee-quote request against a product with no matching `FeeRule` row returns
`"NoFeeConfigured"` — a real, honest answer, not a 404 and not an invented `0`.

### 2.5 `ProfitSharingTerms` — shape only, deliberately empty, not wired to anything in v1

`ProductCode`, `ProfitShareRatioCustomer`, `ProfitShareRatioInstitution`, `EffectiveFrom`/`EffectiveTo` —
the shape a Mudarabah-style savings product would eventually need
(`research/sharia-product-rules.md` Q1). No endpoint reads or writes this table in v1; it exists so the
later task that actually builds Mudarabah product support has a named table to extend rather than
inventing the shape from scratch, exactly the same reasoning `KycVerification` existed, unused, before
anything populated it.

## 3. Endpoints (v1)

| Method | Route | Scope | Purpose |
|---|---|---|---|
| `POST` | `/v1/products` | `product.catalog.write` | Create a product catalog entry |
| `GET` | `/v1/products/{productCode}` | `product.catalog.read` | Fetch a product's current definition and limits |
| `POST` | `/v1/account-bindings` | `product.catalog.write` | Bind an account to a product (`OrchestratorApi`'s account-opening step 5) |
| `GET` | `/v1/account-bindings/{accountNumber}` | `product.catalog.read` | Which product an account is bound to |
| `POST` | `/v1/fee-quotes` | `product.pricing.read` | Given a product + operation type + amount, return the applicable fee — or `NoFeeConfigured` |

No endpoint here ever posts to `LedgerApi` or calls `Garbati.IdentityApi`. `OrchestratorApi` is the only
real caller this service expects in v1.

## 4. What "done" looks like for the first build task

- `Product`, `ProductLimitPolicy` (seeded with explicit placeholder values, same discipline as
  `KycTierPolicy`), `AccountProductBinding`, `FeeRule` and `ProfitSharingTerms` (both unseeded) tables
  exist via EF Core migrations.
- The five endpoints above exist, scoped correctly, idempotent where they create something
  (`POST /v1/account-bindings` needs a client-scoped idempotency key, same convention as every other
  "create a new thing" endpoint on this platform).
- `POST /v1/fee-quotes` against any real product returns `NoFeeConfigured` — tested and asserted
  directly, not just "doesn't throw."
- No code anywhere computes a real fee amount or a real profit-sharing split. If a reviewer finds
  anything that does, that's a scope violation of this task, not a feature.

## 5. Open questions (explicitly deferred, not silently skipped)

- **Every number and rule flagged in `research/sharia-product-rules.md`'s own "needs scholar
  confirmation" section** — not re-listed here; read that document directly. This service's `FeeRule`/
  `ProfitSharingTerms` tables stay empty until that review happens.
- **Real Nigerian minimum-balance/allowed-operations figures** — placeholder only, same open item
  `KycTierPolicy`'s own placeholder data already carries, needs compliance confirmation.
- **How `OrchestratorApi` actually calls this service during account opening and a transfer** — this
  design only specifies this service's own contract; wiring `OrchestratorApi` to call it (adding the real
  step 5 to account opening, adding a fee leg to a transfer's chain) is separate, future work on
  `OrchestratorApi`'s side, not started by this task.
- **The reconciliation job** `ARCHITECTURE.md` already flags ("a reconciliation job should compare posted
  fees with ProductApi's rules") — meaningless until real fee rules exist; not designed here.

## Appendix A. T-045 build notes (foundation: project skeleton, 5 entities, 5 endpoints, migrations)

Built everything in this document's v1 scope in one task, on `feature/productapi-foundation`, mirroring
`Garbati.StaffIdentityApi`'s scaffold exactly (the simplest sibling service — calls nothing else, same as
this one) for every cross-cutting concern: `global.json`/`dotnet-tools.json`, `.gitignore`/`.dockerignore`,
the two-project `.sln`, the four-stage `Dockerfile`, and `ci.yml`'s `test`+`docker` jobs (no `deploy` job
yet, matching how every sibling service started). The judgment calls this task made, beyond what Sections
1-5 above already pin down:

1. **`POST /v1/products` has no idempotency key; `POST /v1/account-bindings` does.** Task file's own
   framing, taken at face value: product creation is a rarer, ops-driven catalog action, not the
   "literal create-a-new-thing shape every other idempotent-create endpoint on this platform uses" —
   the 409-on-duplicate-currently-effective-row check already stops the obvious double-create failure
   mode without one. Binding an account to a product is exactly that literal shape (it's the thing
   `OrchestratorApi`'s account-opening step 5 will retry on a resumed operation), so it gets the full
   client-scoped idempotency-key treatment `StaffUser`/`Customer`/`AccountOpening` all already use.
2. **`Product` is effective-dated, and "currently effective" is enforced two ways.** A partial unique
   index on `(product_code)` filtered to `effective_to IS NULL` is the database-level half (mirrors
   `Garbati.IdentityApi`'s own partial-unique-index idiom for `Mandate`, filtered on a state marker for
   the same reason a Postgres index can't filter on `now()`); `ProductService`'s own check in
   application code additionally catches the rarer case of an existing row whose `EffectiveTo` is set
   but still in the future. Both paths throw the same `DuplicateProductCodeException` → 409.
3. **`AccountProductBinding.AccountNumber` is unique — one binding per account in v1.** `docs/DESIGN.md`
   Section 3's own wording for the GET endpoint ("which product **(if any)** an account is bound to") is
   singular, and nothing in this task's scope describes a re-binding flow that would make multiple
   historical bindings per account meaningful yet. A second bind attempt against an already-bound
   account, under a *different* idempotency key than the one that created it, is a new
   `AccountAlreadyBoundException` → 409 — not explicitly specified, but the only reading consistent with
   "every create-something-new endpoint returns 409 on a real conflict" (`ARCHITECTURE.md` Section 5)
   rather than letting an unhandled `DbUpdateException` surface as a 500.
4. **Distinguishing "idempotent replay" from "genuine conflict" does not rely on which unique constraint
   Postgres reports first.** A retry with the same idempotency key and the same account number violates
   *both* of `AccountProductBinding`'s unique indexes at once, and Postgres does not guarantee it reports
   the idempotency-key one first. `AccountProductBindingService.CreateAsync` checks for an existing
   `(CreatedByClientId, IdempotencyKey)` match FIRST, inside the catch block, regardless of which
   constraint actually fired — only when no such match exists does the account-number constraint mean
   what it's named for (a different key colliding with an already-bound account). Caught by this
   service's own `CreateAccountBinding_RetryWithTheSameKeyAndBody_ReturnsTheSameBinding` test during
   development: it initially returned 409 instead of replaying the original 200.
5. **`FeeQuoteResponse`'s `FeeResult` vocabulary (`NoFeeConfigured`/`FeeApplied`) is this task's own
   naming** — `docs/DESIGN.md` names the first value in prose but not the second. `FeeApplied` marks the
   "a real `FeeRule` row was found and a fee was computed" branch that the task file explicitly invited
   building "for completeness" — it is real, reachable code, but no migration in this repository and no
   test anywhere ever inserts a row that branch could read. See `FeeQuoteService`'s own class comment for
   why this is not the same thing as deciding a real fee rule.
6. **`ProductAuditLog` is one generic table, not a per-entity shape like `StaffAuditLog`.** This service
   audits two different entity types (`Product`, `AccountProductBinding`) from day one, so the
   `EntityType`/`EntityId` generic shape `Garbati.IdentityApi.Entities.AuditEvent` already uses was the
   closer precedent to copy than `StaffIdentityApi`'s single-entity-type `StaffAuditLog`.
7. **Test strategy: controller-level (HTTP), against the real app and a real Testcontainers Postgres,
   nothing mocked.** Same reasoning as `StaffIdentityApi`'s own build notes: this service calls nothing
   downstream (`docs/DESIGN.md` Section 1), so there is no `Clients/`-style fake to maintain. The two
   tests the task file calls out by name as non-negotiable —
   `FeeRulesTable_IsProvablyEmpty_AfterMigration` and `ProfitSharingTermsTable_IsProvablyEmpty_
   AfterMigration` in `Data/MigrationTests.cs` — run a real `SELECT count(*)` against the migrated
   database, not an assumption that "no migration seeds it" is good enough.
8. **Two QA Should-fix findings on PR #1, resolved on the same branch before merge:**
   - **Coverage gap closed:** item 3 above (`AccountAlreadyBoundException` → 409, a *different*
     idempotency key colliding with `AccountNumber` alone, not a replay) depended on the hardcoded
     constraint-name string `ix_account_product_bindings_account_number` actually matching what
     Postgres reports, and had no test. `AccountBindingsControllerTests.
     CreateAccountBinding_DifferentKey_SameAccountAlreadyBoundToAnotherProduct_Returns409` now exercises
     it directly over real HTTP, asserting the 409 status, the `ProblemDetails` title/detail text, and
     that the original binding is unchanged - not just "not 200."
   - **Defensive-coding inconsistency fixed:** `ProductService.CreateAsync`'s `catch (DbUpdateException)`
     had no Postgres-error-code guard, unlike `AccountProductBindingService.CreateAsync`'s precise
     `when (ex.InnerException is PostgresException { SqlState: "23505" })`. Made them match - only a
     real unique-constraint collision becomes `DuplicateProductCodeException` → 409; any other
     `DbUpdateException` (a transient connection failure, say) now propagates as itself, which the
     generic exception handler correctly maps to 500. Proving this against a real, different
     `DbUpdateException` on live Postgres on demand isn't practical, so
     `Services/ProductServiceTests.cs` proves the guard at the service level instead: a `ProductDbContext`
     subclass (`ThrowingOnSaveProductDbContext`) that always throws a `DbUpdateException` whose
     `InnerException` is deliberately not a `PostgresException` at all from `SaveChangesAsync`, run
     against this fixture's own real, already-migrated Testcontainers Postgres for everything up to that
     point (no new package dependency, no second database provider - only the save itself is faked).

See the PR description for how this was tested, the exact passing test count, what could not be
verified, and the full LEARNING_LOG entry.
