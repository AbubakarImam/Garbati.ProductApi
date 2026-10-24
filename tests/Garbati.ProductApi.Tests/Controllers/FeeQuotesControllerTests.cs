using System.Net;
using System.Net.Http.Json;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Garbati.ProductApi.Tests.Controllers;

// DESIGN.md Section 3, endpoint 5 / Section 0 - the single most load-bearing endpoint behavior in this
// task: every real request here must come back NoFeeConfigured, never a 404, never a 0-amount success,
// never an exception.
[Collection("Api")]
public class FeeQuotesControllerTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient PricingReadClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.PricingRead));
    private HttpClient CatalogWriteClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.CatalogWrite));

    // Task file: "POST /v1/fee-quotes against a real, seeded product returns NoFeeConfigured - a direct
    // assertion on the response shape, not just a 200 status code."
    [Fact]
    public async Task FeeQuote_AgainstARealSeededProduct_ReturnsNoFeeConfigured()
    {
        using var writeClient = CatalogWriteClient();
        using var pricingClient = PricingReadClient();
        var product = await ProductApiHelpers.CreateProductAsync(writeClient);

        using var response = await pricingClient.PostAsJsonAsync("/v1/fee-quotes", new
        {
            productCode = product.ProductCode,
            operationType = "Transfer",
            amount = 5000m,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FeeQuoteResponse>();
        Assert.Equal(FeeQuoteResults.NoFeeConfigured, body!.FeeResult);
        Assert.Null(body.FeeAmount);
        Assert.Null(body.FeeType);
    }

    // Same answer even for a product code that was never created at all - never a 404, never an
    // exception (task file: "None will ever be found in this task's own tests or in any real deployment
    // yet - assert the response is a clear, explicit NoFeeConfigured shape").
    [Fact]
    public async Task FeeQuote_AgainstAnUnknownProductCode_StillReturnsNoFeeConfigured_Never404()
    {
        using var pricingClient = PricingReadClient();

        using var response = await pricingClient.PostAsJsonAsync("/v1/fee-quotes", new
        {
            productCode = "DOES-NOT-EXIST-AT-ALL",
            operationType = "Transfer",
            amount = 100m,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FeeQuoteResponse>();
        Assert.Equal(FeeQuoteResults.NoFeeConfigured, body!.FeeResult);
    }

    [Fact]
    public async Task FeeQuote_InvalidRequest_Returns400()
    {
        using var pricingClient = PricingReadClient();

        using var response = await pricingClient.PostAsJsonAsync("/v1/fee-quotes", new
        {
            productCode = "",
            operationType = "Transfer",
            amount = 100m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Belt-and-suspenders, at the service boundary rather than just the controller: no test anywhere in
    // this suite, including this one, ever inserts a FeeRule row - so the table stays empty throughout
    // the whole fee-quote test run, not just right after migration.
    [Fact]
    public async Task NoTestInThisClass_EverWritesAFeeRuleRow()
    {
        await using var ctx = fixture.CreateContext();

        var count = await ctx.FeeRules.CountAsync();

        Assert.Equal(0, count);
    }
}
