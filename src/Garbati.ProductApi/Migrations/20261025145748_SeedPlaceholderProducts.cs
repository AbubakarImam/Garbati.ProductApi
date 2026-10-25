using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garbati.ProductApi.Migrations
{
    // Coordinator fix, 25 Oct 2026 (found while reviewing T-046 on Garbati.OrchestratorApi): the existing
    // SeedPlaceholderProductLimitPolicies migration seeds ProductLimitPolicy rows for 'NG-SAV-STD' and
    // 'NG-CUR-STD', but no migration ever created the matching Product catalog rows those limit policies
    // reference - Products is only ever populated via POST /v1/products. That left two orphaned limit
    // policies pointing at product codes that don't exist as real, bindable Product rows, so
    // POST /v1/account-bindings 404s (ProductNotFoundException) for both of them today. This migration
    // closes that gap the same way the limit-policy seed already does: PLACEHOLDER DATA, not a real,
    // confirmed product catalog - these two rows exist only to prove the catalog/binding shape works
    // end to end, and to match the already-seeded limit-policy placeholders. Replace with confirmed
    // product definitions before relying on this table for anything real.
    /// <inheritdoc />
    public partial class SeedPlaceholderProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO products (product_code, product_name, jurisdiction_code, currency_code, product_type, effective_from, effective_to, created_at)
                VALUES
                    ('NG-SAV-STD', 'Standard Savings (placeholder)', 'NG', 'NGN', 'Savings', '2026-01-01T00:00:00Z', NULL, '2026-01-01T00:00:00Z'),
                    ('NG-CUR-STD', 'Standard Current (placeholder)', 'NG', 'NGN', 'Current', '2026-01-01T00:00:00Z', NULL, '2026-01-01T00:00:00Z')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM products
                WHERE product_code IN ('NG-SAV-STD', 'NG-CUR-STD') AND effective_from = '2026-01-01T00:00:00Z';
                """);
        }
    }
}
