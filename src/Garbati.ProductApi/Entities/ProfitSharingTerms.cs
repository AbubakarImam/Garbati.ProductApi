namespace Garbati.ProductApi.Entities;

// DESIGN.md Section 2.5 / Section 0: shape only, deliberately empty, AND NOT WIRED TO ANYTHING IN V1.
// No endpoint, service method, controller action or validator anywhere in this codebase reads or writes
// this table - it exists purely as a named table shape for a future task (the one that actually builds
// Mudarabah-style profit-sharing product support) to extend, exactly the same reasoning
// Garbati.IdentityApi.Entities.KycVerification existed, unused, before anything populated it.
//
// If a future change in THIS repository ever adds code that reads or writes ProfitSharingTerms, that is
// a scope violation of T-045, not a feature - see docs/DESIGN.md Section 4's own "if a reviewer finds
// anything that does, that's a scope violation of this task, not a feature."
public class ProfitSharingTerms
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = null!;
    public decimal ProfitShareRatioCustomer { get; set; }
    public decimal ProfitShareRatioInstitution { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
