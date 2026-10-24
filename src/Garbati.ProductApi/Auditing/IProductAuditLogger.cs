using Garbati.ProductApi.Entities;

namespace Garbati.ProductApi.Auditing;

public interface IProductAuditLogger
{
    // Stages an audit row on the current DbContext; it is written by the caller's next SaveChanges, so
    // it commits (or rolls back) together with the change it describes (ARCHITECTURE.md Section 5:
    // "written in the same database transaction as the change it describes").
    ProductAuditLog Record(ProductAuditLog entry);
}
