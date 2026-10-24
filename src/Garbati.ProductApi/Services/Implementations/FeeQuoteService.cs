using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Data;
using Garbati.ProductApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Garbati.ProductApi.Services.Implementations;

// docs/DESIGN.md Section 0/2.4/3, endpoint 5. THE load-bearing rule of this whole task: no FeeRule row
// is EVER seeded (no migration in this repository inserts one, and no test writes one either) because
// no qualified Islamic finance scholar has signed off on a real fee rule yet
// (research/sharia-product-rules.md). Every real caller of this endpoint, today and until a future task
// changes that, gets FeeQuoteResults.NoFeeConfigured - never a 404, never a 0-amount success dressed up
// as "the fee is zero", never an unhandled exception.
//
// This class ALSO contains the "a FeeRule row was actually found" branch (task file: "If you want to
// also implement the ... branch for completeness ... that's fine"). That branch is real code, reachable
// in principle, but it is NOT exercised by any test in this task and CANNOT happen against this
// repository's own migrations - it exists only so a future task that starts seeding real fee rules does
// not also have to redesign this response shape. Do not read its presence as this task deciding any
// real fee amount or rule - it decides nothing; it only describes how an eventual real row WOULD be
// read, once one exists.
public class FeeQuoteService(ProductDbContext dbContext) : IFeeQuoteService
{
    public async Task<FeeQuoteResponse> GetQuoteAsync(FeeQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var candidates = await dbContext.FeeRules.AsNoTracking()
            .Where(f => f.ProductCode == request.ProductCode && f.OperationType == request.OperationType)
            .ToListAsync(cancellationToken);

        var feeRule = candidates.FirstOrDefault(f => ProductService.IsCurrentlyEffective(f.EffectiveFrom, f.EffectiveTo, now));

        // The one real answer: nothing configures a fee for this (ProductCode, OperationType) today.
        if (feeRule is null)
        {
            return new FeeQuoteResponse(FeeQuoteResults.NoFeeConfigured, FeeAmount: null, FeeType: null);
        }

        // UNTESTED, CANNOT-CURRENTLY-HAPPEN-FOR-REAL BRANCH (see the class comment above) - no row this
        // branch could ever read exists anywhere this task creates.
        var computedAmount = feeRule.FeeType switch
        {
            "Flat" => feeRule.Amount ?? 0m,
            "Percentage" => request.Amount * (feeRule.Amount ?? 0m) / 100m,
            _ => 0m, // "None"
        };

        return new FeeQuoteResponse(FeeQuoteResults.FeeApplied, computedAmount, feeRule.FeeType);
    }
}
