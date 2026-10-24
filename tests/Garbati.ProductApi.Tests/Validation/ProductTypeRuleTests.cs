using Garbati.ProductApi.Validation;

namespace Garbati.ProductApi.Tests.Validation;

public class ProductTypeRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Loan")]
    [InlineData("savings")]
    public void Validate_RejectsMissingOrUnknownValues(string? productType)
    {
        Assert.NotNull(ProductTypeRule.Validate(productType));
    }

    [Theory]
    [InlineData("Savings")]
    [InlineData("Current")]
    public void Validate_AcceptsTheTwoKnownValues(string productType)
    {
        Assert.Null(ProductTypeRule.Validate(productType));
    }
}
