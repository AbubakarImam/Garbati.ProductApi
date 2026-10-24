using Garbati.ProductApi.Contracts.Requests;

namespace Garbati.ProductApi.Validation;

public class CreateAccountProductBindingRequestValidator
{
    public IReadOnlyList<string> Validate(CreateAccountProductBindingRequest request)
    {
        var errors = new List<string>();

        if (IdempotencyKeyRule.Validate(request.IdempotencyKey) is { } idempotencyError)
            errors.Add(idempotencyError);

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            errors.Add("AccountNumber is required.");

        if (string.IsNullOrWhiteSpace(request.ProductCode))
            errors.Add("ProductCode is required.");

        return errors;
    }
}
