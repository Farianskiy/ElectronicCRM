using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddManualPriceCalculationComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selection_source",
                table: "catalog_price_calculation_line_components",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Recommended");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selection_source",
                table: "catalog_price_calculation_line_components");
        }
    }
}
