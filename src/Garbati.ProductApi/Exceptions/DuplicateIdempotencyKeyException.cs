namespace Garbati.ProductApi.Exceptions;

// Same convention as LedgerApi's/IdentityApi's/OrchestratorApi's/StaffIdentityApi's exception of the
// same name: a reused idempotency key whose request body does not match the original is refused (409),
// rather than silently returning someone else's created binding (ARCHITECTURE.md Section 5: "same key
// with a different body returns 409").
public class DuplicateIdempotencyKeyException : Exception
{
    public DuplicateIdempotencyKeyException()
    {
    }

    public DuplicateIdempotencyKeyException(string message)
        : base(message)
    {
    }

    public DuplicateIdempotencyKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
