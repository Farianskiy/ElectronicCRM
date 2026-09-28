using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "product_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "MainProduct");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_types_kind",
                table: "product_types",
                sql: "\"kind\" IN ('MainProduct', 'Component')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_product_types_kind",
                table: "product_types");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "product_types");
        }
    }
}
