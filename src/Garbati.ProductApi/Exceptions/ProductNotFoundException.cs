namespace Garbati.ProductApi.Exceptions;

// Raised when a request names a ProductCode that does not exist at all (DESIGN.md Section 3,
// POST /v1/account-bindings: "404 if ProductCode doesn't exist").
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException()
    {
    }

    public ProductNotFoundException(string message)
        : base(message)
    {
    }

    public ProductNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
