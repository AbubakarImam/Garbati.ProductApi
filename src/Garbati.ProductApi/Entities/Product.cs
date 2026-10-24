namespace Garbati.ProductApi.Entities;

// DESIGN.md Section 2.1: the catalog entry. ProductType is a plain string column, not an enum table -
// task file: "Savings/Current - a plain string column is fine, no enum table needed for two values."
//
// Effective-dated the same way KycTierPolicy already is (DESIGN.md Section 2.1: "so a product's terms
// can change over time without losing the history of what applied when"). This means more than one row
// can exist for the same ProductCode over time; at most one may be currently effective at a time -
// Data/ProductDbContext.cs' partial unique index on (ProductCode) WHERE effective_to IS NULL is the
// real, database-level half of that rule (POST /v1/products' own check is the other half - see
// Services/Implementations/ProductService.cs).
public class Product
{
    public long Id { get; set; }

    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string JurisdictionCode { get; set; } = null!;
    public string CurrencyCode { get; set; } = null!;
    public string ProductType { get; set; } = null!;

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
