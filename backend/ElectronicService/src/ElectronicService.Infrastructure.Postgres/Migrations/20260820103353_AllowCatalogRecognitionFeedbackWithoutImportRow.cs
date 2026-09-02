using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AllowCatalogRecognitionFeedbackWithoutImportRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_feedback_import_links",
                table: "catalog_recognition_feedback");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_feedback_import_links",
                table: "catalog_recognition_feedback",
                sql: "\"import_row_id\" IS NULL OR \"import_batch_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_feedback_import_links",
                table: "catalog_recognition_feedback");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_feedback_import_links",
                table: "catalog_recognition_feedback",
                sql: "(\"import_batch_id\" IS NULL AND \"import_row_id\" IS NULL) OR (\"import_batch_id\" IS NOT NULL AND \"import_row_id\" IS NOT NULL)");
        }
    }
}
