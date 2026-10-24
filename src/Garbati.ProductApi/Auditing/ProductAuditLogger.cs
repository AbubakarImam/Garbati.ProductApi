using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Data;
using Garbati.ProductApi.Entities;

namespace Garbati.ProductApi.Auditing;

public class ProductAuditLogger(ProductDbContext dbContext, ICurrentClient currentClient) : IProductAuditLogger
{
    public ProductAuditLog Record(ProductAuditLog entry)
    {
        entry.ActorId ??= currentClient.ClientId;
        entry.CreatedAt = DateTimeOffset.UtcNow;
        dbContext.ProductAuditLogs.Add(entry);
        return entry;
    }
}
