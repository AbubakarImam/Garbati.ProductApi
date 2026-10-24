namespace Garbati.ProductApi.Contracts.Responses;

// DESIGN.md Section 3, endpoints 3/4.
public record AccountProductBindingResponse(string AccountNumber, string ProductCode, DateTimeOffset BoundAt);
