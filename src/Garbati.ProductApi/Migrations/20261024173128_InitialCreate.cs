using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Garbati.ProductApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_product_bindings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    bound_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    created_by_client_id = table.Column<string>(type: "text", nullable: true),
                    request_hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_product_bindings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fee_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    operation_type = table.Column<string>(type: "text", nullable: false),
                    fee_type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_rules", x => x.id);
                    table.CheckConstraint("ck_fee_rules_fee_type", "\"fee_type\" IN ('Flat', 'Percentage', 'None')");
                });

            migrationBuilder.CreateTable(
                name: "product_audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_limit_policies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    minimum_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allowed_operations = table.Column<string>(type: "jsonb", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_limit_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    jurisdiction_code = table.Column<string>(type: "text", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    product_type = table.Column<string>(type: "text", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_product_type", "\"product_type\" IN ('Savings', 'Current')");
                });

            migrationBuilder.CreateTable(
                name: "profit_sharing_terms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    profit_share_ratio_customer = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    profit_share_ratio_institution = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profit_sharing_terms", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_product_bindings_account_number",
                table: "account_product_bindings",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_product_bindings_created_by_client_id_idempotency_k",
                table: "account_product_bindings",
                columns: new[] { "created_by_client_id", "idempotency_key" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_account_product_bindings_product_code",
                table: "account_product_bindings",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix_fee_rules_product_code_operation_type_effective_from",
                table: "fee_rules",
                columns: new[] { "product_code", "operation_type", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_logs_entity_type_entity_id",
                table: "product_audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_limit_policies_product_code_effective_from",
                table: "product_limit_policies",
                columns: new[] { "product_code", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_products_product_code_currently_effective",
                table: "products",
                column: "product_code",
                unique: true,
                filter: "\"effective_to\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_profit_sharing_terms_product_code_effective_from",
                table: "profit_sharing_terms",
                columns: new[] { "product_code", "effective_from" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_product_bindings");

            migrationBuilder.DropTable(
                name: "fee_rules");

            migrationBuilder.DropTable(
                name: "product_audit_logs");

            migrationBuilder.DropTable(
                name: "product_limit_policies");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "profit_sharing_terms");
        }
    }
}
