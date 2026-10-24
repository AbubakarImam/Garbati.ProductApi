using Garbati.ProductApi.Auditing;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Data;
using Garbati.ProductApi.Exceptions;
using Garbati.ProductApi.Services.Implementations;
using Garbati.ProductApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Garbati.ProductApi.Tests.Services;

// QA Should-fix #2: ProductService.CreateAsync's catch clause was widened to match
// AccountProductBindingService's precise `when (ex.InnerException is PostgresException { SqlState:
// "23505" })` guard, so only a real unique-constraint collision becomes a DuplicateProductCodeException
// (409) - any other DbUpdateException (a transient connection failure, say) must propagate as itself,
// which the generic exception handler then maps to the 500 it actually is.
//
// Triggering a genuinely different real DbUpdateException against live Postgres on demand isn't
// practical, so this proves the guard at the service level with a test double: a ProductDbContext
// subclass that always throws a DbUpdateException whose InnerException is deliberately NOT a
// PostgresException at all (let alone one with SqlState 23505) from SaveChangesAsync. It still runs
// against this fixture's own real, already-migrated Testcontainers Postgres for everything up to that
// point (no new package dependency, no second database provider) - only the save itself is faked.
[Collection("Api")]
public class ProductServiceTests(ApiFixture fixture)
{
    [Fact]
    public async Task CreateAsync_ADbUpdateExceptionThatIsNotAUniqueViolation_PropagatesAsItself_NotDuplicateProductCodeException()
    {
        await using var dbContext = new ThrowingOnSaveProductDbContext(fixture.CreateOptions());
        var auditLogger = new ProductAuditLogger(dbContext, new FakeCurrentClient());
        var service = new ProductService(dbContext, auditLogger);

        var request = new CreateProductRequest(
            $"SVC-TEST-{Guid.NewGuid():N}"[..20], "Service Test", "NG", "NGN", "Savings", null, null);

        // Specifically NOT a DuplicateProductCodeException - a non-unique-violation save failure must
        // not be misreported as "this product code already exists."
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(request));
        Assert.IsNotType<DuplicateProductCodeException>(ex);
    }

    private class FakeCurrentClient : ICurrentClient
    {
        public string? ClientId => "test-client";
    }

    private class ThrowingOnSaveProductDbContext(DbContextOptions<ProductDbContext> options) : ProductDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException(
                "Simulated save failure that is NOT a unique-constraint violation.",
                new InvalidOperationException("Simulated transient failure - deliberately not a PostgresException."));
    }
}
