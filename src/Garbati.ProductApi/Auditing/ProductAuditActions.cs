namespace Garbati.ProductApi.Auditing;

// The two audited actions in this task (task file Section 5). No action exists for a fee-quote lookup -
// it changes nothing - and none exists for ProfitSharingTerms, which no code anywhere touches.
public static class ProductAuditActions
{
    public const string ProductCreated = "ProductCreated";
    public const string AccountProductBindingCreated = "AccountProductBindingCreated";
}
