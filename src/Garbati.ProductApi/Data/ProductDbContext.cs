using System.Text.Json;
using Garbati.ProductApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Garbati.ProductApi.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductLimitPolicy> ProductLimitPolicies => Set<ProductLimitPolicy>();
    public DbSet<AccountProductBinding> AccountProductBindings => Set<AccountProductBinding>();

    // FeeRule/ProfitSharingTerms get real DbSets - EF needs them to create the tables via migration -
    // but nothing in Services/ ever queries ProfitSharingTerms (docs/DESIGN.md Section 0/2.5). FeeRule
    // IS queried, by Services/Implementations/FeeQuoteService.cs's lookup - just never written to by any
    // code in this task.
    public DbSet<FeeRule> FeeRules => Set<FeeRule>();
    public DbSet<ProfitSharingTerms> ProfitSharingTerms => Set<ProfitSharingTerms>();

    public DbSet<ProductAuditLog> ProductAuditLogs => Set<ProductAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Copied directly from Garbati.IdentityApi.Data.IdentityDbContext's own stringListConverter/
        // stringListComparer for KycTierPolicy.RequiredEvidence (task file: "read that entity and its
        // IdentityDbContext configuration directly before implementing this, copy the pattern, don't
        // reinvent it").
        var stringListConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            // DESIGN.md 2.1: effective-dated, so more than one row per ProductCode can exist over time.
            // This partial unique index is the DB-level half of "409 if ProductCode already has a
            // currently-effective row" - mirrors Garbati.IdentityApi's own partial-unique-index idiom for
            // Mandate (ix_mandates_customer_account_active, filtered on a state marker rather than a
            // live time comparison, for the same reason: Postgres indexes cannot filter on now()).
            // EffectiveTo IS NULL is this service's "currently open-ended" marker - the application check
            // in ProductService additionally handles the (expected to be rare, in v1) case of a row with
            // a future EffectiveTo.
            entity.HasIndex(p => p.ProductCode)
                .IsUnique()
                .HasDatabaseName("ix_products_product_code_currently_effective")
                .HasFilter("\"effective_to\" IS NULL");

            entity.ToTable(t => t.HasCheckConstraint("ck_products_product_type", "\"product_type\" IN ('Savings', 'Current')"));
        });

        // DESIGN.md Section 2.2 / decision #11 (IdentityApi precedent): AllowedOperations kept as a
        // flexible jsonb string list, not fixed columns - the exact pattern KycTierPolicy.
        // RequiredEvidence already uses.
        modelBuilder.Entity<ProductLimitPolicy>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.ProductCode, p.EffectiveFrom });
            entity.Property(p => p.MinimumBalance).HasPrecision(18, 2);
            entity.Property(p => p.AllowedOperations).HasConversion(stringListConverter).HasColumnType("jsonb")
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<AccountProductBinding>(entity =>
        {
            entity.HasKey(b => b.Id);

            // Judgment call (Entities/AccountProductBinding.cs's own comment, PR description): one
            // binding per account in v1.
            entity.HasIndex(b => b.AccountNumber).IsUnique();
            entity.HasIndex(b => b.ProductCode);

            // Same idempotency scheme as every other create-something-new endpoint on this platform
            // (ARCHITECTURE.md Section 5: "unique per calling client"; StaffUser's own identical index).
            entity.HasIndex(b => new { b.CreatedByClientId, b.IdempotencyKey }).IsUnique().AreNullsDistinct(false);
        });

        // Shape only - DESIGN.md Section 2.4/Section 0. No seed data, ever (task file). The check
        // constraint still documents the real, closed vocabulary FeeType is allowed to take, even though
        // no row exists to constrain yet.
        modelBuilder.Entity<FeeRule>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => new { f.ProductCode, f.OperationType, f.EffectiveFrom });
            entity.Property(f => f.Amount).HasPrecision(18, 2);
            entity.ToTable(t => t.HasCheckConstraint("ck_fee_rules_fee_type", "\"fee_type\" IN ('Flat', 'Percentage', 'None')"));
        });

        // Shape only, not wired to anything - DESIGN.md Section 2.5/Section 0. No seed data, ever, and
        // no code anywhere in Services/ or Controllers/ references this DbSet.
        modelBuilder.Entity<Entities.ProfitSharingTerms>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.ProductCode, p.EffectiveFrom });
            entity.Property(p => p.ProfitShareRatioCustomer).HasPrecision(5, 4);
            entity.Property(p => p.ProfitShareRatioInstitution).HasPrecision(5, 4);
        });

        modelBuilder.Entity<ProductAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });
    }
}
