using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "product_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_changed_by",
                table: "product_audit_entries",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_product_date",
                table: "product_audit_entries",
                columns: new[] { "product_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_product_id",
                table: "product_audit_entries",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_audit_entries_source_id",
                table: "product_audit_entries",
                column: "source_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_audit_entries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "products");
        }
    }
}
