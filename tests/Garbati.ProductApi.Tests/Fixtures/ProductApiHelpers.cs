using System.Net.Http.Json;
using Garbati.ProductApi.Contracts.Responses;

namespace Garbati.ProductApi.Tests.Fixtures;

// Shared request-building helpers so every test file doesn't re-derive the same JSON shapes.
public static class ProductApiHelpers
{
    public static async Task<ProductResponse> CreateProductAsync(
        HttpClient catalogWriteClient,
        string? productCode = null,
        string productType = "Savings",
        string jurisdictionCode = "NG",
        string currencyCode = "NGN")
    {
        productCode ??= $"TEST-{Guid.NewGuid():N}"[..20];

        var request = new
        {
            productCode,
            productName = "Test Product",
            jurisdictionCode,
            currencyCode,
            productType,
        };

        var response = await catalogWriteClient.PostAsJsonAsync("/v1/products", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    public static async Task<AccountProductBindingResponse> CreateAccountBindingAsync(
        HttpClient catalogWriteClient,
        string productCode,
        string? accountNumber = null,
        string? idempotencyKey = null)
    {
        accountNumber ??= $"ACC-{Guid.NewGuid():N}"[..20];
        idempotencyKey ??= $"test-bind-{Guid.NewGuid():N}";

        var request = new { idempotencyKey, accountNumber, productCode };

        var response = await catalogWriteClient.PostAsJsonAsync("/v1/account-bindings", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountProductBindingResponse>())!;
    }
}
