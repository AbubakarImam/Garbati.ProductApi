using Garbati.ProductApi.Contracts.Requests;

namespace Garbati.ProductApi.Validation;

public class CreateProductRequestValidator
{
    public IReadOnlyList<string> Validate(CreateProductRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ProductCode))
            errors.Add("ProductCode is required.");

        if (string.IsNullOrWhiteSpace(request.ProductName))
            errors.Add("ProductName is required.");

        if (string.IsNullOrWhiteSpace(request.JurisdictionCode))
            errors.Add("JurisdictionCode is required.");

        if (string.IsNullOrWhiteSpace(request.CurrencyCode))
            errors.Add("CurrencyCode is required.");

        if (ProductTypeRule.Validate(request.ProductType) is { } productTypeError)
            errors.Add(productTypeError);

        if (request.EffectiveFrom is not null && request.EffectiveTo is not null && request.EffectiveTo <= request.EffectiveFrom)
            errors.Add("EffectiveTo must be after EffectiveFrom.");

        return errors;
    }
}
