using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_import_columns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_column_number = table.Column<int>(type: "integer", nullable: false),
                    source_header = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_source_header = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    target_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_columns", x => x.id);
                    table.CheckConstraint("ck_catalog_import_columns_characteristic_mapping", "(\"target_kind\" = 'Characteristic' AND \"characteristic_definition_id\" IS NOT NULL) OR (\"target_kind\" <> 'Characteristic' AND \"characteristic_definition_id\" IS NULL)");
                    table.CheckConstraint("ck_catalog_import_columns_confidence", "\"confidence\" >= 0 AND \"confidence\" <= 1");
                    table.CheckConstraint("ck_catalog_import_columns_number", "\"source_column_number\" > 0");
                    table.CheckConstraint("ck_catalog_import_columns_target_not_none", "\"target_kind\" <> 'None'");
                    table.CheckConstraint("ck_catalog_import_columns_unmapped_not_confirmed", "\"target_kind\" <> 'Unmapped' OR \"is_confirmed\" = FALSE");
                    table.ForeignKey(
                        name: "FK_catalog_import_columns_catalog_import_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "catalog_import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_import_columns_characteristic_definitions_character~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_import_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    raw_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    normalized_data_json = table.Column<string>(type: "jsonb", nullable: false),
                    issues_json = table.Column<string>(type: "jsonb", nullable: false),
                    warnings_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_rows", x => x.id);
                    table.CheckConstraint("ck_catalog_import_rows_number", "\"row_number\" >= 2");
                    table.CheckConstraint("ck_catalog_import_rows_status_not_none", "\"status\" <> 'None'");
                    table.ForeignKey(
                        name: "FK_catalog_import_rows_catalog_import_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "catalog_import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_columns_batch_target",
                table: "catalog_import_columns",
                columns: new[] { "batch_id", "target_kind" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_columns_characteristic",
                table: "catalog_import_columns",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_import_columns_batch_number",
                table: "catalog_import_columns",
                columns: new[] { "batch_id", "source_column_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_rows_batch_status_number",
                table: "catalog_import_rows",
                columns: new[] { "batch_id", "status", "row_number" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_import_rows_batch_number",
                table: "catalog_import_rows",
                columns: new[] { "batch_id", "row_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_import_columns");

            migrationBuilder.DropTable(
                name: "catalog_import_rows");
        }
    }
}
