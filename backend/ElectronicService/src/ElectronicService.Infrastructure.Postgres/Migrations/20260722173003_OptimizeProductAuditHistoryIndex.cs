using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeProductAuditHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_audit_entries_product_date",
                table: "product_audit_entries");

            migrationBuilder.DropIndex(
                name: "ix_product_audit_entries_product_id",
                table: "product_audit_entries");

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_product_history",
                table: "product_audit_entries",
                columns: new[] { "product_id", "changed_at_utc", "id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_audit_entries_product_history",
                table: "product_audit_entries");

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_product_date",
                table: "product_audit_entries",
                columns: new[] { "product_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_product_id",
                table: "product_audit_entries",
                column: "product_id");
        }
    }
}
