namespace Garbati.ProductApi.Entities;

// DESIGN.md Section 2.3: links a LedgerApi account (by its account number) to a ProductCode. This
// service never calls LedgerApi to validate AccountNumber exists - OrchestratorApi already confirmed
// that during the step before this one (DESIGN.md 2.3).
//
// IdempotencyKey/CreatedByClientId/RequestHash are infrastructure for POST /v1/account-bindings'
// idempotency requirement (ARCHITECTURE.md Section 5), the same split Garbati.StaffIdentityApi.Entities.
// StaffUser uses for its own idempotent-create fields beyond what its design doc's entity prose names.
//
// Judgment call (full reasoning in the PR description): AccountNumber is unique - at most one binding
// per account in v1. DESIGN.md's own wording for the GET endpoint ("which product (if any) an account
// is bound to") is singular, and nothing in this task's scope describes a re-binding flow to make
// multiple historical bindings per account meaningful yet.
public class AccountProductBinding
{
    public long Id { get; set; }

    public string AccountNumber { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public DateTimeOffset BoundAt { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? CreatedByClientId { get; set; }
    public string? RequestHash { get; set; }
}
