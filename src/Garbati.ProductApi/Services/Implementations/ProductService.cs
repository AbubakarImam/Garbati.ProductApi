using Garbati.ProductApi.Auditing;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Data;
using Garbati.ProductApi.Entities;
using Garbati.ProductApi.Exceptions;
using Garbati.ProductApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Garbati.ProductApi.Services.Implementations;

public class ProductService(ProductDbContext dbContext, IProductAuditLogger auditLogger) : IProductService
{
    // "Currently effective" everywhere in this service means: EffectiveFrom has arrived, and EffectiveTo
    // is either not set or hasn't arrived yet. Shared so Product/ProductLimitPolicy/FeeRule lookups all
    // apply the exact same rule.
    public static bool IsCurrentlyEffective(DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, DateTimeOffset now) =>
        effectiveFrom <= now && (effectiveTo is null || effectiveTo > now);

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var effectiveFrom = request.EffectiveFrom ?? now;

        // Task file, POST /v1/products: "409 if ProductCode already has a currently-effective row." The
        // database's own partial unique index (Data/ProductDbContext.cs) covers the common EffectiveTo
        // IS NULL case; this also catches the rarer case of an existing row whose EffectiveTo is set but
        // still in the future.
        var currentlyEffective = await dbContext.Products
            .Where(p => p.ProductCode == request.ProductCode)
            .ToListAsync(cancellationToken);
        if (currentlyEffective.Any(p => IsCurrentlyEffective(p.EffectiveFrom, p.EffectiveTo, now)))
        {
            throw new DuplicateProductCodeException(
                $"Product '{request.ProductCode}' already has a currently-effective row.");
        }

        var product = new Product
        {
            ProductCode = request.ProductCode,
            ProductName = request.ProductName,
            JurisdictionCode = request.JurisdictionCode,
            CurrencyCode = request.CurrencyCode,
            ProductType = request.ProductType,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = request.EffectiveTo,
            CreatedAt = now,
        };

        await dbContext.Products.AddAsync(product, cancellationToken);
        auditLogger.Record(new ProductAuditLog
        {
            Action = ProductAuditActions.ProductCreated,
            EntityType = nameof(Product),
            EntityId = product.ProductCode,
            Details = $"ProductType={product.ProductType}, JurisdictionCode={product.JurisdictionCode}",
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        // QA Should-fix #2: guard on the actual Postgres unique-violation error code, the same precise
        // pattern AccountProductBindingService.CreateAsync already uses - a DbUpdateException that is NOT
        // the partial unique index firing (e.g. a transient connection failure) must propagate as the
        // generic 500 it actually is, not get misreported as a 409 just because *some* DbUpdateException
        // happened during this save.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // The partial unique index fired - a concurrent request won the race. Same conflict, same 409.
            throw new DuplicateProductCodeException(
                $"Product '{request.ProductCode}' already has a currently-effective row.");
        }

        return await ToResponseAsync(product, cancellationToken);
    }

    public async Task<ProductResponse?> GetAsync(string productCode, CancellationToken cancellationToken = default)
    {
        // DESIGN.md Section 3, endpoint 2: "404 if no product with that code exists at all" - any row,
        // not just a currently-effective one. Among possibly several historical rows for the same code,
        // prefer the currently open-ended one, else the most recently effective.
        var product = await dbContext.Products.AsNoTracking()
            .Where(p => p.ProductCode == productCode)
            .OrderByDescending(p => p.EffectiveTo == null)
            .ThenByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? null : await ToResponseAsync(product, cancellationToken);
    }

    private async Task<ProductResponse> ToResponseAsync(Product product, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Task file, GET /v1/products/{productCode}: "if the product exists but has no currently-
        // effective limit policy, return the product with null/empty limit fields rather than a 500."
        var limitPolicy = await dbContext.ProductLimitPolicies.AsNoTracking()
            .Where(p => p.ProductCode == product.ProductCode)
            .ToListAsync(cancellationToken);
        var currentPolicy = limitPolicy.FirstOrDefault(p => IsCurrentlyEffective(p.EffectiveFrom, p.EffectiveTo, now));

        return new ProductResponse(
            product.ProductCode,
            product.ProductName,
            product.JurisdictionCode,
            product.CurrencyCode,
            product.ProductType,
            product.EffectiveFrom,
            product.EffectiveTo,
            currentPolicy?.MinimumBalance,
            currentPolicy?.AllowedOperations);
    }
}
