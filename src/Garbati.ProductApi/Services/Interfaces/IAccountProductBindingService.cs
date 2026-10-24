using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;

namespace Garbati.ProductApi.Services.Interfaces;

public interface IAccountProductBindingService
{
    Task<AccountProductBindingResponse> CreateAsync(CreateAccountProductBindingRequest request, CancellationToken cancellationToken = default);
    Task<AccountProductBindingResponse?> GetAsync(string accountNumber, CancellationToken cancellationToken = default);
}
