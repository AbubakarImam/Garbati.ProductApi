namespace Garbati.ProductApi.Contracts.Responses;

// DESIGN.md Section 3, endpoints 1/2: the product plus its currently-effective ProductLimitPolicy's
// MinimumBalance/AllowedOperations, in one response. MinimumBalance/AllowedOperations are null/empty
// when the product has no currently-effective limit policy yet - task file: "a product predating any
// limit policy being configured is a real possible state, not a bug."
public record ProductResponse(
    string ProductCode,
    string ProductName,
    string JurisdictionCode,
    string CurrencyCode,
    string ProductType,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal? MinimumBalance,
    List<string>? AllowedOperations);
