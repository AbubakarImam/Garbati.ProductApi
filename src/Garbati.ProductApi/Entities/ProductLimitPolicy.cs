namespace Garbati.ProductApi.Entities;

// DESIGN.md Section 2.2 / task file: AllowedOperations is a configurable jsonb string list, the exact
// same "configurable, not hardcoded" shape Garbati.IdentityApi.Entities.KycTierPolicy.RequiredEvidence
// already uses (decision #11 there) - see Data/ProductDbContext.cs for the identical value-converter
// pattern, copied rather than reinvented. Seeded only with explicit placeholder values (Migrations/
// *SeedPlaceholderProductLimitPolicies*) - the real Nigerian minimum-balance figures still need
// compliance confirmation (research/nigerian-regulatory-requirements.md), the same open item
// KycTierPolicy's own placeholder data already carries.
public class ProductLimitPolicy
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = null!;
    public decimal MinimumBalance { get; set; }
    public List<string> AllowedOperations { get; set; } = [];

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
