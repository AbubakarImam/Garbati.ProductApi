namespace Garbati.ProductApi.Validation;

// DESIGN.md Section 2.1: "Savings | Current for v1 - a real, mechanical distinction, not a Sharia
// judgment call." Task file: "a plain string column is fine, no enum table needed for two values" - so,
// unlike StaffIdentityApi's RoleRule, this validates against a plain string list rather than parsing a
// C# enum, matching how the entity itself stores ProductType (Entities/Product.cs).
public static class ProductTypeRule
{
    public static readonly IReadOnlyList<string> AllowedValues = ["Savings", "Current"];

    public static string? Validate(string? productType) =>
        string.IsNullOrWhiteSpace(productType) ? "ProductType is required."
        : !AllowedValues.Contains(productType) ? $"ProductType must be one of: {string.Join(", ", AllowedValues)}."
        : null;
}
