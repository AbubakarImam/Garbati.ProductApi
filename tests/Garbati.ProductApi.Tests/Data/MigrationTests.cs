using Garbati.ProductApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Garbati.ProductApi.Tests.Data;

// Task file's migration test requirements, verbatim:
// "Migration applies cleanly; ProductLimitPolicy's two placeholder rows exist afterward with the exact
// values the seed migration specifies; FeeRule and ProfitSharingTerms are provably empty tables after
// migration (a real SELECT count(*) assertion, not an assumption)."
//
// This is the single most load-bearing test file in this task - FeeRule/ProfitSharingTerms_IsEmpty below
// are the direct database-emptiness assertions the task explicitly calls for.
[Collection("Api")]
public class MigrationTests(ApiFixture fixture)
{
    [Fact]
    public async Task AppliedMigrations_IncludeInitialCreateAndSeedPlaceholderProductLimitPolicies()
    {
        await using var ctx = fixture.CreateContext();

        var applied = await ctx.Database.GetAppliedMigrationsAsync();

        Assert.Contains(applied, m => m.EndsWith("InitialCreate"));
        Assert.Contains(applied, m => m.EndsWith("SeedPlaceholderProductLimitPolicies"));
    }

    [Fact]
    public async Task ProductLimitPolicy_PlaceholderRows_ExistWithTheExactSeededValues()
    {
        await using var ctx = fixture.CreateContext();

        var savings = await ctx.ProductLimitPolicies.AsNoTracking()
            .SingleAsync(p => p.ProductCode == ApiFixture.SeededSavingsProductCode);
        Assert.Equal(1000.00m, savings.MinimumBalance);
        Assert.Equal(new List<string> { "View", "Credit", "Debit" }, savings.AllowedOperations);
        Assert.Null(savings.EffectiveTo);

        var current = await ctx.ProductLimitPolicies.AsNoTracking()
            .SingleAsync(p => p.ProductCode == ApiFixture.SeededCurrentProductCode);
        Assert.Equal(0.00m, current.MinimumBalance);
        Assert.Equal(new List<string> { "View", "Credit", "Debit" }, current.AllowedOperations);
        Assert.Null(current.EffectiveTo);
    }

    // THE direct database-emptiness assertion the task file calls for by name - not an assumption that
    // "no migration seeds it" is enough, an actual SELECT count(*) against the real migrated database.
    [Fact]
    public async Task FeeRulesTable_IsProvablyEmpty_AfterMigration()
    {
        await using var ctx = fixture.CreateContext();

        var count = await ctx.FeeRules.CountAsync();

        Assert.Equal(0, count);
    }

    // Same assertion, for the other table docs/DESIGN.md Section 0 says must never be seeded or touched.
    [Fact]
    public async Task ProfitSharingTermsTable_IsProvablyEmpty_AfterMigration()
    {
        await using var ctx = fixture.CreateContext();

        var count = await ctx.ProfitSharingTerms.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ProductsTable_ExistsWithItsExpectedColumns()
    {
        await using var ctx = fixture.CreateContext();

        await AssertColumnsAsync(ctx, "products",
            "id", "product_code", "product_name", "jurisdiction_code", "currency_code", "product_type",
            "effective_from", "effective_to", "created_at");
    }

    [Fact]
    public async Task ProductLimitPoliciesTable_ExistsWithItsExpectedColumns()
    {
        await using var ctx = fixture.CreateContext();

        await AssertColumnsAsync(ctx, "product_limit_policies",
            "id", "product_code", "minimum_balance", "allowed_operations", "effective_from", "effective_to");
    }

    [Fact]
    public async Task AccountProductBindingsTable_ExistsWithItsExpectedColumns()
    {
        await using var ctx = fixture.CreateContext();

        await AssertColumnsAsync(ctx, "account_product_bindings",
            "id", "account_number", "product_code", "bound_at",
            "idempotency_key", "created_by_client_id", "request_hash");
    }

    [Fact]
    public async Task FeeRulesTable_ExistsWithItsExpectedColumns()
    {
        await using var ctx = fixture.CreateContext();

        await AssertColumnsAsync(ctx, "fee_rules",
            "id", "product_code", "operation_type", "fee_type", "amount", "effective_from", "effective_to");
    }

    [Fact]
    public async Task ProfitSharingTermsTable_ExistsWithItsExpectedColumns()
    {
        await using var ctx = fixture.CreateContext();

        await AssertColumnsAsync(ctx, "profit_sharing_terms",
            "id", "product_code", "profit_share_ratio_customer", "profit_share_ratio_institution",
            "effective_from", "effective_to");
    }

    private static async Task AssertColumnsAsync(Microsoft.EntityFrameworkCore.DbContext ctx, string table, params string[] expectedColumns)
    {
        var actual = new List<string>();
        var connection = ctx.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name = @table";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table";
        parameter.Value = table;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            actual.Add(reader.GetString(0));
        }

        foreach (var expected in expectedColumns)
        {
            Assert.Contains(expected, actual);
        }
    }
}
