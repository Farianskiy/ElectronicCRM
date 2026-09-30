using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "import_mode",
                table: "catalog_import_batches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "CreateOnly");

            migrationBuilder.Sql(
                "ALTER TABLE catalog_import_batches ALTER COLUMN import_mode DROP DEFAULT;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_import_batches_import_mode_not_none",
                table: "catalog_import_batches",
                sql: "\"import_mode\" <> 'None'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_import_batches_import_mode_not_none",
                table: "catalog_import_batches");

            migrationBuilder.DropColumn(
                name: "import_mode",
                table: "catalog_import_batches");
        }
    }
}
