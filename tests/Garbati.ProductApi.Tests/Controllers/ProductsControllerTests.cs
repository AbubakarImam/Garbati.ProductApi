using System.Net;
using System.Net.Http.Json;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Tests.Fixtures;

namespace Garbati.ProductApi.Tests.Controllers;

// DESIGN.md Section 3, endpoints 1/2.
[Collection("Api")]
public class ProductsControllerTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CatalogWriteClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.CatalogWrite));
    private HttpClient CatalogReadClient() => fixture.CreateClient(ApiFixture.OnlyClient(ProductScopes.CatalogRead));

    // Task file: "POST /v1/products then GET /v1/products/{productCode} round-trips correctly,
    // including its limit policy fields when one exists" - this product's code matches one of the
    // seed migration's own placeholder ProductLimitPolicy rows, so a currently-effective policy exists.
    [Fact]
    public async Task CreateThenGetProduct_RoundTrips_IncludingLimitPolicyFields_WhenOneExists()
    {
        using var writeClient = CatalogWriteClient();
        using var readClient = CatalogReadClient();

        using var createResponse = await writeClient.PostAsJsonAsync("/v1/products", new
        {
            productCode = ApiFixture.SeededSavingsProductCode,
            productName = "Standard Savings",
            jurisdictionCode = "NG",
            currencyCode = "NGN",
            productType = "Savings",
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(ApiFixture.SeededSavingsProductCode, created!.ProductCode);

        using var getResponse = await readClient.GetAsync($"/v1/products/{ApiFixture.SeededSavingsProductCode}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Standard Savings", fetched!.ProductName);
        Assert.Equal("NG", fetched.JurisdictionCode);
        Assert.Equal("NGN", fetched.CurrencyCode);
        Assert.Equal("Savings", fetched.ProductType);
        Assert.Equal(1000.00m, fetched.MinimumBalance);
        Assert.Equal(new List<string> { "View", "Credit", "Debit" }, fetched.AllowedOperations);
    }

    // Task file: "...and when one doesn't" - a product code that has no seeded (or any other)
    // ProductLimitPolicy row must still come back as a real 200, not a 500 - task file: "a product
    // predating any limit policy being configured is a real possible state, not a bug."
    [Fact]
    public async Task CreateThenGetProduct_RoundTrips_WithNullLimitFields_WhenNoPolicyExists()
    {
        using var writeClient = CatalogWriteClient();
        using var readClient = CatalogReadClient();
        var productCode = $"NO-POLICY-{Guid.NewGuid():N}"[..20];

        using var createResponse = await writeClient.PostAsJsonAsync("/v1/products", new
        {
            productCode,
            productName = "No Policy Product",
            jurisdictionCode = "NG",
            currencyCode = "NGN",
            productType = "Current",
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var getResponse = await readClient.GetAsync($"/v1/products/{productCode}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Null(fetched!.MinimumBalance);
        Assert.Null(fetched.AllowedOperations);
    }

    [Fact]
    public async Task GetProduct_Returns404_WhenProductDoesNotExistAtAll()
    {
        using var readClient = CatalogReadClient();

        using var response = await readClient.GetAsync("/v1/products/DOES-NOT-EXIST-AT-ALL");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_SameCurrentlyEffectiveProductCodeTwice_Returns409()
    {
        using var writeClient = CatalogWriteClient();
        var productCode = $"DUPE-{Guid.NewGuid():N}"[..20];
        var body = new
        {
            productCode,
            productName = "First",
            jurisdictionCode = "NG",
            currencyCode = "NGN",
            productType = "Savings",
        };

        using var first = await writeClient.PostAsJsonAsync("/v1/products", body);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await writeClient.PostAsJsonAsync("/v1/products", body with { productName = "Second" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_InvalidProductType_Returns400()
    {
        using var writeClient = CatalogWriteClient();

        using var response = await writeClient.PostAsJsonAsync("/v1/products", new
        {
            productCode = $"BAD-TYPE-{Guid.NewGuid():N}"[..20],
            productName = "Bad Type",
            jurisdictionCode = "NG",
            currencyCode = "NGN",
            productType = "CurrentAccountButWrong",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
