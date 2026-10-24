using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;

namespace Garbati.ProductApi.Services.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetAsync(string productCode, CancellationToken cancellationToken = default);
}
