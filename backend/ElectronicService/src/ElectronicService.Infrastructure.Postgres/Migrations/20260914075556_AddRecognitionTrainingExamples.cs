using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionTrainingExamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_training_examples",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    raw_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    span_start = table.Column<int>(type: "integer", nullable: false),
                    span_length = table.Column<int>(type: "integer", nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_training_examples", x => x.id);
                    table.CheckConstraint("ck_recognition_training_example_name", "char_length(btrim(\"product_name\")) > 0");
                    table.CheckConstraint("ck_recognition_training_example_raw", "char_length(btrim(\"raw_value\")) > 0");
                    table.CheckConstraint("ck_recognition_training_example_revocation", "(\"revoked_at_utc\" IS NULL AND \"revoked_by_user_id\" IS NULL) OR (\"revoked_at_utc\" IS NOT NULL AND \"revoked_by_user_id\" IS NOT NULL AND \"revoked_at_utc\" >= \"confirmed_at_utc\")");
                    table.CheckConstraint("ck_recognition_training_example_span", "\"span_start\" >= 0 AND \"span_length\" > 0");
                    table.CheckConstraint("ck_recognition_training_example_value", "char_length(btrim(\"normalized_value\")) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_recognition_training_example_scope",
                table: "catalog_recognition_training_examples",
                columns: new[] { "manufacturer_id", "product_type_id", "characteristic_definition_id", "confirmed_at_utc" },
                filter: "\"revoked_at_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recognition_training_example_active_source",
                table: "catalog_recognition_training_examples",
                column: "source_feedback_id",
                unique: true,
                filter: "\"revoked_at_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_training_examples");
        }
    }
}
