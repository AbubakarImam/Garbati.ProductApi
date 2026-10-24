namespace Garbati.ProductApi.Contracts.Requests;

// DESIGN.md Section 3, endpoint 5.
public record FeeQuoteRequest(string ProductCode, string OperationType, decimal Amount);
