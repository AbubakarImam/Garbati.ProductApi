using System.Net;
using System.Text;
using System.Text.Json;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Middleware;
using Garbati.ProductApi.Tests.Fixtures;

namespace Garbati.ProductApi.Tests.Authorization;

// Task file: "Scope enforcement: each endpoint rejects a caller with the wrong scope (403) and no API
// key at all (401)." Same test shape as LedgerApi's/IdentityApi's/OrchestratorApi's/StaffIdentityApi's
// AuthorizationTests (StaffIdentityApi's own table-driven style, named in the task file).
[Collection("Api")]
public class AuthorizationTests(ApiFixture fixture)
{
    private static string CreateProductBody() => """
        {"productCode":"AUTHZ-TEST","productName":"Authz Test","jurisdictionCode":"NG","currencyCode":"NGN","productType":"Savings"}
        """;
    private static string CreateBindingBody() => """
        {"idempotencyKey":"authz-test-bind","accountNumber":"ACC-AUTHZ-TEST","productCode":"AUTHZ-TEST"}
        """;
    private static string FeeQuoteBody() => """{"productCode":"AUTHZ-TEST","operationType":"Transfer","amount":100}""";

    public static TheoryData<string, string, string?, string> Endpoints => new()
    {
        { "POST", "/v1/products", CreateProductBody(), ProductScopes.CatalogWrite },
        { "GET", "/v1/products/DOES-NOT-EXIST", null, ProductScopes.CatalogRead },
        { "POST", "/v1/account-bindings", CreateBindingBody(), ProductScopes.CatalogWrite },
        { "GET", "/v1/account-bindings/ACC-DOES-NOT-EXIST", null, ProductScopes.CatalogRead },
        { "POST", "/v1/fee-quotes", FeeQuoteBody(), ProductScopes.PricingRead },
    };

    private static HttpRequestMessage Request(string method, string path, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        return request;
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Returns401_WithProblemDetails_WhenNoApiKeyIsSent(string method, string path, string? body, string scope)
    {
        _ = scope;
        using var client = fixture.CreateClient(clientId: null);

        using var response = await client.SendAsync(Request(method, path, body));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ApiKeyAuthenticationHandler.SchemeName, response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var correlationId = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.Equal(correlationId, problem.RootElement.GetProperty("correlationId").GetString());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Returns403_WhenTheKeyLacksTheRequiredScope(string method, string path, string? body, string scope)
    {
        using var client = fixture.CreateClient(ApiFixture.AllButClient(scope));

        using var response = await client.SendAsync(Request(method, path, body));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task ReachesTheEndpoint_WhenTheKeyHoldsOnlyTheRequiredScope(string method, string path, string? body, string scope)
    {
        using var client = fixture.CreateClient(ApiFixture.OnlyClient(scope));

        using var response = await client.SendAsync(Request(method, path, body));

        // Past authorization the controller answers normally (200/404/409 depending on the data) - never 403.
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Returns401_ForAnUnknownApiKey()
    {
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, "idk_not_a_real_key");

        using var response = await client.GetAsync("/v1/products/DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
