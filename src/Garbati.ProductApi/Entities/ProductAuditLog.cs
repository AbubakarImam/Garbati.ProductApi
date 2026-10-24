namespace Garbati.ProductApi.Entities;

// ARCHITECTURE.md Section 5's audit convention ("written in the same database transaction as the change
// it describes, append-only"), applied here with a generic EntityType/EntityId shape - this service
// audits two different kinds of create events (Product, AccountProductBinding), the same reasoning
// Garbati.IdentityApi.Entities.AuditEvent uses rather than StaffAuditLog's single-entity-type shape.
//
// Task file Section 5: "One audit row per Product creation and per AccountProductBinding creation ...
// No audit events needed for GET endpoints or for the (always-NoFeeConfigured-today) fee-quote lookup,
// which changes nothing."
public class ProductAuditLog
{
    public long Id { get; set; }

    // One of Auditing/ProductAuditActions.cs's constants.
    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;

    // The calling API client id (there is no human "acting staff member" concept in this service's own
    // requests - OrchestratorApi is the only caller, DESIGN.md Section 1).
    public string? ActorId { get; set; }

    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
