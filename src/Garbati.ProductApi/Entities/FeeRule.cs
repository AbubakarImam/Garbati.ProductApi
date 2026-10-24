namespace Garbati.ProductApi.Entities;

// DESIGN.md Section 2.4 / Section 0: shape only, deliberately empty. "No row is ever seeded for this
// table" - see Migrations (there is no FeeRule seed migration, by design) and Services/Implementations/
// FeeQuoteService.cs's own comment. Amount is nullable because it is meaningless when FeeType is None.
//
// This is a real, queryable table - the "found a real FeeRule row and compute the fee" branch in
// FeeQuoteService exists and is reachable in principle - but nothing in this repository, in any test,
// or in any real deployment yet inserts a row here. See docs/DESIGN.md Section 0 for why: no qualified
// Islamic finance scholar has signed off on a real fee rule, so none is invented.
public class FeeRule
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = null!;
    public string OperationType { get; set; } = null!;
    public string FeeType { get; set; } = null!;
    public decimal? Amount { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
