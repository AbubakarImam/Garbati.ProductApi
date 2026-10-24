using Garbati.ProductApi.Contracts.Requests;

namespace Garbati.ProductApi.Validation;

public class FeeQuoteRequestValidator
{
    public IReadOnlyList<string> Validate(FeeQuoteRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ProductCode))
            errors.Add("ProductCode is required.");

        if (string.IsNullOrWhiteSpace(request.OperationType))
            errors.Add("OperationType is required.");

        if (request.Amount < 0)
            errors.Add("Amount must not be negative.");

        return errors;
    }
}
