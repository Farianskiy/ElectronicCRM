using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentNeedRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "default_quantity_per_unit",
                table: "component_need_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "requirement_mode",
                table: "component_need_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Optional");

            migrationBuilder.AddCheckConstraint(
                name: "ck_component_need_default_quantity",
                table: "component_need_definitions",
                sql: "\"default_quantity_per_unit\" >= 1 AND \"default_quantity_per_unit\" <= 1000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_component_need_default_quantity",
                table: "component_need_definitions");

            migrationBuilder.DropColumn(
                name: "default_quantity_per_unit",
                table: "component_need_definitions");

            migrationBuilder.DropColumn(
                name: "requirement_mode",
                table: "component_need_definitions");
        }
    }
}
