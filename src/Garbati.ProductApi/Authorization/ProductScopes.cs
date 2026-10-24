namespace Garbati.ProductApi.Authorization;

// Each scope is also the name of the authorization policy that requires it (same pattern as
// LedgerApi.Authorization.LedgerScopes / Garbati.IdentityApi.Authorization.IdentityScopes /
// Garbati.OrchestratorApi.Authorization.OrchestratorScopes / Garbati.StaffIdentityApi.Authorization.
// StaffIdentityScopes).
//
// DESIGN.md Section 3: three scopes. Catalog read/write is split from pricing read because
// OrchestratorApi (this service's one real caller) legitimately needs read access to pricing without
// needing to write catalog entries - the same "different blast radii for the same caller" reasoning
// StaffIdentityScopes already applies to its own two scopes.
public static class ProductScopes
{
    public const string ClaimType = "scope";

    public const string CatalogRead = "product.catalog.read";
    public const string CatalogWrite = "product.catalog.write";
    public const string PricingRead = "product.pricing.read";

    public static readonly IReadOnlyList<string> All = [CatalogRead, CatalogWrite, PricingRead];
}
