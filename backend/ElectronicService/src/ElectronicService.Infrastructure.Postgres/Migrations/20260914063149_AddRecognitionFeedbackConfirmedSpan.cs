using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionFeedbackConfirmedSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "confirmed_raw_value",
                table: "catalog_recognition_feedback",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "confirmed_span_length",
                table: "catalog_recognition_feedback",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "confirmed_span_start",
                table: "catalog_recognition_feedback",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_feedback_confirmed_span",
                table: "catalog_recognition_feedback",
                sql: "(\"confirmed_raw_value\" IS NULL AND \"confirmed_span_start\" IS NULL AND \"confirmed_span_length\" IS NULL) OR (\"confirmed_raw_value\" IS NOT NULL AND char_length(btrim(\"confirmed_raw_value\")) > 0 AND \"confirmed_span_start\" IS NOT NULL AND \"confirmed_span_start\" >= 0 AND \"confirmed_span_length\" IS NOT NULL AND \"confirmed_span_length\" > 0 AND \"final_normalized_value\" IS NOT NULL AND \"feedback_type\" IN ('Accepted', 'Corrected', 'AddedManually', 'ConflictResolved'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_feedback_confirmed_span",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "confirmed_raw_value",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "confirmed_span_length",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "confirmed_span_start",
                table: "catalog_recognition_feedback");
        }
    }
}
