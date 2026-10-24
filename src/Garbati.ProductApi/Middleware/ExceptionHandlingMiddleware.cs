using Garbati.ProductApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Garbati.ProductApi.Middleware;

// Same shape as LedgerApi's/IdentityApi's/OrchestratorApi's/StaffIdentityApi's middleware of the same
// name (ARCHITECTURE.md Section 5's status-code mapping). 401/403 are handled by
// ApiKeyAuthenticationHandler, not here.
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, title) = Map(ex);
            var isExpected = status != StatusCodes.Status500InternalServerError;

            if (isExpected)
                logger.LogWarning(ex, "Request rejected ({Status}) {Method} {Path}", status, context.Request.Method, context.Request.Path);
            else
                logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = status;

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Only expected business errors expose their message; unexpected ones must not leak internals.
                Detail = isExpected ? ex.Message : null,
                Extensions = { ["correlationId"] = context.TraceIdentifier }
            };

            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }

    private static (int Status, string Title) Map(Exception ex) => ex switch
    {
        ProductNotFoundException => (StatusCodes.Status404NotFound, "Product not found."),
        AccountBindingNotFoundException => (StatusCodes.Status404NotFound, "Account binding not found."),
        DuplicateProductCodeException => (StatusCodes.Status409Conflict, "A currently-effective product with this code already exists."),
        AccountAlreadyBoundException => (StatusCodes.Status409Conflict, "This account is already bound to a product."),
        DuplicateIdempotencyKeyException => (StatusCodes.Status409Conflict, "Duplicate idempotency key."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
