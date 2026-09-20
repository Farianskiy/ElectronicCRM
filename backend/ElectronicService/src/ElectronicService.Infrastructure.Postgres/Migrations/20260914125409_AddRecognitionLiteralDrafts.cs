using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionLiteralDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_literal_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    literal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    generator_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matched_name_count = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_literal_drafts", x => x.id);
                    table.CheckConstraint("ck_literal_draft_generator", "char_length(btrim(\"generator_version\")) > 0");
                    table.CheckConstraint("ck_literal_draft_literal", "char_length(btrim(\"literal\")) > 0");
                    table.CheckConstraint("ck_literal_draft_matched_names", "\"matched_name_count\" > 0");
                    table.CheckConstraint("ck_literal_draft_value", "char_length(btrim(\"normalized_value\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "catalog_recognition_literal_draft_evidence",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_example_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_supporting = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_literal_draft_evidence", x => new { x.draft_id, x.training_example_id });
                    table.ForeignKey(
                        name: "FK_catalog_recognition_literal_draft_evidence_catalog_recognit~",
                        column: x => x.draft_id,
                        principalTable: "catalog_recognition_literal_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_literal_draft_evidence_catalog_recogni~1",
                        column: x => x.training_example_id,
                        principalTable: "catalog_recognition_training_examples",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_literal_draft_evidence_example",
                table: "catalog_recognition_literal_draft_evidence",
                column: "training_example_id");

            migrationBuilder.CreateIndex(
                name: "ix_literal_draft_scope",
                table: "catalog_recognition_literal_drafts",
                columns: new[] { "manufacturer_id", "product_type_id", "characteristic_definition_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_literal_draft_evidence");

            migrationBuilder.DropTable(
                name: "catalog_recognition_literal_drafts");
        }
    }
}
