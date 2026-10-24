namespace Garbati.ProductApi.Contracts.Requests;

// DESIGN.md Section 3, endpoint 3. Unlike POST /v1/products above, this one DOES need a client-scoped
// idempotency key - task file: "it's the literal 'create a new thing' shape every other
// idempotent-create endpoint on this platform already uses."
public record CreateAccountProductBindingRequest(
    string IdempotencyKey,
    string AccountNumber,
    string ProductCode) : IIdempotentRequest;
