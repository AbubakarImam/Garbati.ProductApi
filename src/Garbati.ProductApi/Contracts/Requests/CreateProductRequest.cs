namespace Garbati.ProductApi.Contracts.Requests;

// DESIGN.md Section 3, endpoint 1. No idempotency key - judgment call, see the PR description: this is
// a rarer, ops-driven catalog action (not the literal "create a new thing" shape every other
// idempotent-create endpoint on this platform uses), and the 409-on-duplicate-currently-effective-row
// check already stops the obvious "retry created it twice" failure mode without one.
public record CreateProductRequest(
    string ProductCode,
    string ProductName,
    string JurisdictionCode,
    string CurrencyCode,
    string ProductType,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo);
