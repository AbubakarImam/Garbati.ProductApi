namespace Garbati.ProductApi.Exceptions;

// Judgment call (Entities/AccountProductBinding.cs's own comment, PR description): AccountNumber is
// unique - at most one binding per account in v1. Raised when an account already has a DIFFERENT
// binding (a different idempotency key than the one that originally created it) - not the same thing as
// DuplicateIdempotencyKeyException, which covers a retried key whose body no longer matches.
public class AccountAlreadyBoundException : Exception
{
    public AccountAlreadyBoundException()
    {
    }

    public AccountAlreadyBoundException(string message)
        : base(message)
    {
    }

    public AccountAlreadyBoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
