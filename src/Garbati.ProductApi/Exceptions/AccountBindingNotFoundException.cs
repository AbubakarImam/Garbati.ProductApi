namespace Garbati.ProductApi.Exceptions;

// DESIGN.md Section 3, GET /v1/account-bindings/{accountNumber}: "404 if no binding exists for that
// account number."
public class AccountBindingNotFoundException : Exception
{
    public AccountBindingNotFoundException()
    {
    }

    public AccountBindingNotFoundException(string message)
        : base(message)
    {
    }

    public AccountBindingNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
