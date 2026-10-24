using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;

namespace Garbati.ProductApi.Services.Interfaces;

public interface IFeeQuoteService
{
    Task<FeeQuoteResponse> GetQuoteAsync(FeeQuoteRequest request, CancellationToken cancellationToken = default);
}
