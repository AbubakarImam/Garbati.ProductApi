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
