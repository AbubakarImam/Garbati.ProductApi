using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Garbati.ProductApi.Tests.Controllers;

// DESIGN.md Section 3, endpoints 3/4.
[Collection("Api")]
public class AccountBindingsControllerTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CatalogWriteClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.CatalogWrite));
    private HttpClient CatalogReadClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.CatalogRead));

    [Fact]
    public async Task CreateThenGetAccountBinding_RoundTrips()
    {
        using var writeClient = CatalogWriteClient();
        using var readClient = CatalogReadClient();
        var product = await ProductApiHelpers.CreateProductAsync(writeClient);
        var accountNumber = $"ACC-{Guid.NewGuid():N}"[..20];

        using var createResponse = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = $"bind-{Guid.NewGuid():N}",
            accountNumber,
            productCode = product.ProductCode,
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var getResponse = await readClient.GetAsync($"/v1/account-bindings/{accountNumber}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AccountProductBindingResponse>();
        Assert.Equal(accountNumber, fetched!.AccountNumber);
        Assert.Equal(product.ProductCode, fetched.ProductCode);
    }

    // Task file: "idempotency key behavior (same key + same body returns the same binding ...)".
    [Fact]
    public async Task CreateAccountBinding_RetryWithTheSameKeyAndBody_ReturnsTheSameBinding()
    {
        using var writeClient = CatalogWriteClient();
        var product = await ProductApiHelpers.CreateProductAsync(writeClient);
        var key = $"bind-retry-{Guid.NewGuid():N}";
        var request = new
        {
            idempotencyKey = key,
            accountNumber = $"ACC-{Guid.NewGuid():N}"[..20],
            productCode = product.ProductCode,
        };

        using var first = await writeClient.PostAsJsonAsync("/v1/account-bindings", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<AccountProductBindingResponse>();

        using var second = await writeClient.PostAsJsonAsync("/v1/account-bindings", request);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<AccountProductBindingResponse>();
        Assert.Equal(firstBody!.AccountNumber, secondBody!.AccountNumber);
        Assert.Equal(firstBody.BoundAt, secondBody.BoundAt);

        await using var ctx = fixture.CreateContext();
        var count = await ctx.AccountProductBindings.CountAsync(b => b.AccountNumber == request.accountNumber);
        Assert.Equal(1, count);
    }

    // Task file: "...same key + different body returns 409 - the standard pattern, test it the standard way".
    [Fact]
    public async Task CreateAccountBinding_SameKeyDifferentBody_Returns409()
    {
        using var writeClient = CatalogWriteClient();
        var productOne = await ProductApiHelpers.CreateProductAsync(writeClient);
        var productTwo = await ProductApiHelpers.CreateProductAsync(writeClient);
        var key = $"bind-conflict-{Guid.NewGuid():N}";

        using var first = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = key,
            accountNumber = $"ACC-{Guid.NewGuid():N}"[..20],
            productCode = productOne.ProductCode,
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = key,
            accountNumber = $"ACC-{Guid.NewGuid():N}"[..20],
            productCode = productTwo.ProductCode,
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // QA Should-fix #1: the third branch AccountProductBindingService.CreateAsync's constraint-name
    // check creates - a DIFFERENT idempotency key colliding on AccountNumber alone (not a replay of the
    // same binding) - had no test anywhere. This exercises it directly: bind an account to product A,
    // then attempt to bind the SAME account number to a different product B under a brand-new
    // idempotency key, and assert the real 409/AccountAlreadyBoundException shape, not just "not 200".
    // This is the path that depends on the hardcoded ix_account_product_bindings_account_number
    // constraint name actually matching what Postgres reports - if that ever drifts, this test (not just
    // "not 200") is what would catch it falling through to the bare `throw;` and surfacing as a 500.
    [Fact]
    public async Task CreateAccountBinding_DifferentKey_SameAccountAlreadyBoundToAnotherProduct_Returns409()
    {
        using var writeClient = CatalogWriteClient();
        var productA = await ProductApiHelpers.CreateProductAsync(writeClient);
        var productB = await ProductApiHelpers.CreateProductAsync(writeClient);
        var accountNumber = $"ACC-{Guid.NewGuid():N}"[..20];

        using var first = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = $"bind-first-{Guid.NewGuid():N}",
            accountNumber,
            productCode = productA.ProductCode,
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = $"bind-second-{Guid.NewGuid():N}", // brand-new key - NOT a replay
            accountNumber, // same account number as the binding above
            productCode = productB.ProductCode,
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("This account is already bound to a product.", problem.RootElement.GetProperty("title").GetString());
        Assert.Contains(accountNumber, problem.RootElement.GetProperty("detail").GetString());

        // Still bound to the original product - the rejected second call changed nothing.
        await using var ctx = fixture.CreateContext();
        var binding = await ctx.AccountProductBindings.AsNoTracking().SingleAsync(b => b.AccountNumber == accountNumber);
        Assert.Equal(productA.ProductCode, binding.ProductCode);
    }

    // Task file: "404 for an unknown ProductCode."
    [Fact]
    public async Task CreateAccountBinding_UnknownProductCode_Returns404()
    {
        using var writeClient = CatalogWriteClient();

        using var response = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = $"bind-unknown-{Guid.NewGuid():N}",
            accountNumber = $"ACC-{Guid.NewGuid():N}"[..20],
            productCode = "DOES-NOT-EXIST-AT-ALL",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountBinding_Returns404_WhenNoBindingExists()
    {
        using var readClient = CatalogReadClient();

        using var response = await readClient.GetAsync("/v1/account-bindings/ACC-DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccountBinding_MissingIdempotencyKey_Returns400()
    {
        using var writeClient = CatalogWriteClient();
        var product = await ProductApiHelpers.CreateProductAsync(writeClient);

        using var response = await writeClient.PostAsJsonAsync("/v1/account-bindings", new
        {
            idempotencyKey = "",
            accountNumber = $"ACC-{Guid.NewGuid():N}"[..20],
            productCode = product.ProductCode,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
