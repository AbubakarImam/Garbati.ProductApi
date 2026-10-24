using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Garbati.ProductApi.Auditing;
using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Data;
using Garbati.ProductApi.Entities;
using Garbati.ProductApi.Exceptions;
using Garbati.ProductApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Garbati.ProductApi.Services.Implementations;

public class AccountProductBindingService(
    ProductDbContext dbContext,
    IProductAuditLogger auditLogger,
    ICurrentClient currentClient) : IAccountProductBindingService
{
    // Exact constraint names from Migrations/*InitialCreate* (EFCore.NamingConventions' snake_case
    // naming of EF's default "IX_<Table>_<Columns>" index names) - verified against the generated
    // migration, not guessed, the same discipline Garbati.StaffIdentityApi.Services.Implementations.
    // StaffService's own constants comment calls out.
    // Postgres truncates identifiers over 63 bytes - the EF-generated name for this composite index was
    // cut to "...idempotency_k", verified against the generated migration (not guessed; a wrong name
    // here would silently fall through to the unhandled "throw" branch below and misreport a real
    // idempotency-key replay as a 500).
    private const string IdempotencyKeyConstraintName = "ix_account_product_bindings_created_by_client_id_idempotency_k";
    private const string AccountNumberConstraintName = "ix_account_product_bindings_account_number";

    // Fingerprint of the fields that define the binding being created, so a reused idempotency key with
    // a different body is detected (same scheme as StaffService.HashRequest/CustomerService.HashRequest).
    private static string HashRequest(CreateAccountProductBindingRequest request) => Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { request.AccountNumber, request.ProductCode }))));

    private static AccountProductBindingResponse ToResponse(AccountProductBinding binding) =>
        new(binding.AccountNumber, binding.ProductCode, binding.BoundAt);

    // Postgres "timestamp with time zone" stores microsecond precision (6 fractional digits); a .NET
    // DateTimeOffset tick is 100ns (7 fractional digits, 10 ticks per microsecond). Without this, the
    // value returned on the ORIGINAL create call (kept in memory, never re-read from the database) can
    // differ from the value returned on an idempotent REPLAY (read back from the database, which has
    // already truncated it) by a few hundred nanoseconds - an intermittent CI failure
    // (CreateAccountBinding_RetryWithTheSameKeyAndBody_ReturnsTheSameBinding) caught this. Truncating at
    // the moment of creation means both calls return the exact same value every time.
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMicrosecond, value.Offset);

    public async Task<AccountProductBindingResponse> CreateAsync(CreateAccountProductBindingRequest request, CancellationToken cancellationToken = default)
    {
        // DESIGN.md Section 3, endpoint 3: "404 if ProductCode doesn't exist." This service never calls
        // LedgerApi to check AccountNumber - DESIGN.md 2.3, OrchestratorApi already did that.
        var productExists = await dbContext.Products.AnyAsync(p => p.ProductCode == request.ProductCode, cancellationToken);
        if (!productExists)
        {
            throw new ProductNotFoundException($"Product '{request.ProductCode}' was not found.");
        }

        var clientId = currentClient.ClientId;
        var requestHash = HashRequest(request);

        var binding = new AccountProductBinding
        {
            AccountNumber = request.AccountNumber,
            ProductCode = request.ProductCode,
            BoundAt = TruncateToMicroseconds(DateTimeOffset.UtcNow),
            IdempotencyKey = request.IdempotencyKey,
            CreatedByClientId = clientId,
            RequestHash = requestHash,
        };

        await dbContext.AccountProductBindings.AddAsync(binding, cancellationToken);
        var audit = auditLogger.Record(new ProductAuditLog
        {
            Action = ProductAuditActions.AccountProductBindingCreated,
            EntityType = nameof(AccountProductBinding),
            EntityId = binding.AccountNumber,
            Details = $"ProductCode={binding.ProductCode}",
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx)
        {
            // Dependents before principals (EF navigation-fixup ordering - mirrors StaffService/
            // CustomerService's own identical retry/detach shape).
            dbContext.Entry(binding).State = EntityState.Detached;
            dbContext.Entry(audit).State = EntityState.Detached;

            // A retry of an already-created binding violates BOTH unique constraints at once (same
            // AccountNumber, same CreatedByClientId+IdempotencyKey) - which one Postgres actually reports
            // as pgEx.ConstraintName is not guaranteed to be the idempotency one, so check for an
            // idempotency-key match FIRST, regardless of which constraint fired. Only when no such match
            // exists does AccountNumberConstraintName mean what it's named for: a genuinely different
            // idempotency key trying to bind an account that is already bound to something else.
            var existingByKey = await dbContext.AccountProductBindings.AsNoTracking().FirstOrDefaultAsync(
                b => b.CreatedByClientId == clientId && b.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existingByKey is not null)
            {
                // Same key, different request: refuse rather than report the old binding as this one's
                // (409, ARCHITECTURE.md Section 5).
                if (existingByKey.RequestHash != requestHash)
                    throw new DuplicateIdempotencyKeyException(
                        $"Idempotency key '{request.IdempotencyKey}' was already used for a different request.");

                // Same key, same request: a retry of an already-created binding.
                return ToResponse(existingByKey);
            }

            if (pgEx.ConstraintName == AccountNumberConstraintName)
            {
                throw new AccountAlreadyBoundException($"Account '{request.AccountNumber}' is already bound to a product.");
            }

            if (pgEx.ConstraintName == IdempotencyKeyConstraintName)
                throw new InvalidOperationException("Error retrieving the result of the existing idempotency key.");

            throw;
        }

        return ToResponse(binding);
    }

    public async Task<AccountProductBindingResponse?> GetAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var binding = await dbContext.AccountProductBindings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber, cancellationToken);

        return binding is null ? null : ToResponse(binding);
    }
}
