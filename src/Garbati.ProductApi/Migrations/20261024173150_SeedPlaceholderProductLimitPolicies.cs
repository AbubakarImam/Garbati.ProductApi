using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garbati.ProductApi.Migrations
{
    // PLACEHOLDER DATA - not real, confirmed Nigerian minimum-balance/allowed-operations figures. Task
    // file / docs/DESIGN.md Section 2.2: these two rows exist only to prove the ProductLimitPolicy shape
    // works end to end (the configurable jsonb AllowedOperations list, copied from Garbati.IdentityApi's
    // KycTierPolicy pattern) - exactly the same discipline that repo's own SeedPlaceholderKycTierPolicies
    // migration follows for the same underlying reason (research/nigerian-regulatory-requirements.md
    // flags this general category of number as unverified against a primary CBN circular). Replace these
    // with confirmed values before relying on this table for anything real.
    //
    // UNLIKE this migration, no equivalent seed migration exists for FeeRule or ProfitSharingTerms, and
    // none ever should until a qualified Islamic finance scholar signs off on real content for them
    // (docs/DESIGN.md Section 0) - that is the single most load-bearing boundary in this whole task.
    /// <inheritdoc />
    public partial class SeedPlaceholderProductLimitPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO product_limit_policies (product_code, minimum_balance, allowed_operations, effective_from, effective_to)
                VALUES
                    ('NG-SAV-STD', 1000.00, '["View", "Credit", "Debit"]', '2026-01-01T00:00:00Z', NULL),
                    ('NG-CUR-STD', 0.00, '["View", "Credit", "Debit"]', '2026-01-01T00:00:00Z', NULL)
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM product_limit_policies
                WHERE product_code IN ('NG-SAV-STD', 'NG-CUR-STD') AND effective_from = '2026-01-01T00:00:00Z';
                """);
        }
    }
}
