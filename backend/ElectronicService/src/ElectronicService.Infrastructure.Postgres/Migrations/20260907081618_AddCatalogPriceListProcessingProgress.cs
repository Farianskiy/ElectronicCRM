using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPriceListProcessingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "estimated_rows_count",
                table: "catalog_price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "read_rows_count",
                table: "catalog_price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "saved_rows_count",
                table: "catalog_price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_lists_estimated_rows_count",
                table: "catalog_price_lists",
                sql: "\"estimated_rows_count\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_lists_processing_progress",
                table: "catalog_price_lists",
                sql: "\"saved_rows_count\" <= \"read_rows_count\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_lists_read_rows_count",
                table: "catalog_price_lists",
                sql: "\"read_rows_count\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_lists_saved_rows_count",
                table: "catalog_price_lists",
                sql: "\"saved_rows_count\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_lists_estimated_rows_count",
                table: "catalog_price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_lists_processing_progress",
                table: "catalog_price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_lists_read_rows_count",
                table: "catalog_price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_lists_saved_rows_count",
                table: "catalog_price_lists");

            migrationBuilder.DropColumn(
                name: "estimated_rows_count",
                table: "catalog_price_lists");

            migrationBuilder.DropColumn(
                name: "read_rows_count",
                table: "catalog_price_lists");

            migrationBuilder.DropColumn(
                name: "saved_rows_count",
                table: "catalog_price_lists");
        }
    }
}
