using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations;

[DbContext(typeof(ElectronicDbContext))]
[Migration("20261014193000_AddProductTypeArchiving")]
public sealed class AddProductTypeArchiving : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "archived_at_utc",
            table: "product_types",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_archived",
            table: "product_types",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_product_types_is_archived",
            table: "product_types",
            column: "is_archived");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_product_types_is_archived",
            table: "product_types");

        migrationBuilder.DropColumn(
            name: "archived_at_utc",
            table: "product_types");

        migrationBuilder.DropColumn(
            name: "is_archived",
            table: "product_types");
    }
}
