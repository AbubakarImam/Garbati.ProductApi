# Garbati.ProductApi

GARBATI's product & pricing service — the "Pricing Desk" in the platform's own floor-plan: it decides
what rules, fees and limits apply to a product; it never executes anything. `LedgerApi` posts money,
`Garbati.IdentityApi` decides who's allowed to act, `Garbati.OrchestratorApi` carries a request between
them — this service answers "what are this product's rules," nothing more.

**Status: design only, not yet built.** See [`docs/DESIGN.md`](docs/DESIGN.md) for the full v1 design,
including why its fee and Sharia-related rules are deliberately left empty pending a qualified Islamic
finance scholar's sign-off (`research/sharia-product-rules.md` in the `garbati-platform` hub repo) —
this service's catalog/limits/binding mechanics are real and buildable now; its actual fee amounts and
profit-sharing terms are not invented here.

Part of [GARBATI](https://github.com/AbubakarImam/garbati-platform), an open-source core-banking platform
for Sharia-compliant microfinance.
