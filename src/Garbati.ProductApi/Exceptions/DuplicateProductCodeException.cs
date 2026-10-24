namespace Garbati.ProductApi.Exceptions;

// Task file, POST /v1/products: "409 if ProductCode already has a currently-effective row - the same
// 'don't silently duplicate' discipline as every other create-something-new endpoint on this platform."
public class DuplicateProductCodeException : Exception
{
    public DuplicateProductCodeException()
    {
    }

    public DuplicateProductCodeException(string message)
        : base(message)
    {
    }

    public DuplicateProductCodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
