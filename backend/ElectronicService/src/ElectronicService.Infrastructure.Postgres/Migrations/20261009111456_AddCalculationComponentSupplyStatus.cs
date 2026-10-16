using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationComponentSupplyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "supply_status",
                table: "catalog_price_calculation_line_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ToPurchase");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "supply_status",
                table: "catalog_price_calculation_line_components");
        }
    }
}
