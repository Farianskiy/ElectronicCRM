using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionMultiIntegerDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_multi_integer_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generator_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matched_name_count = table.Column<int>(type: "integer", nullable: false),
                    supporting_name_count = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_multi_integer_drafts", x => x.id);
                    table.CheckConstraint("ck_multi_integer_draft_counts", "\"supporting_name_count\" >= 2 AND \"matched_name_count\" = \"supporting_name_count\" AND \"matched_name_count\" <= 200");
                    table.CheckConstraint("ck_multi_integer_draft_generator", "char_length(btrim(\"generator_version\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "catalog_recognition_multi_integer_draft_evidence",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_example_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_supporting = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_multi_integer_draft_evidence", x => new { x.draft_id, x.training_example_id });
                    table.ForeignKey(
                        name: "FK_catalog_recognition_multi_integer_draft_evidence_catalog_re~",
                        column: x => x.draft_id,
                        principalTable: "catalog_recognition_multi_integer_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_multi_integer_draft_evidence_catalog_r~1",
                        column: x => x.training_example_id,
                        principalTable: "catalog_recognition_training_examples",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_recognition_multi_integer_draft_parts",
                columns: table => new
                {
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    literal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    distinct_value_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_multi_integer_draft_parts", x => new { x.draft_id, x.position });
                    table.CheckConstraint("ck_multi_integer_part_kind", "(\"characteristic_definition_id\" IS NULL AND \"literal\" IS NOT NULL AND char_length(\"literal\") > 0 AND \"distinct_value_count\" = 0) OR (\"characteristic_definition_id\" IS NOT NULL AND \"characteristic_definition_id\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"literal\" IS NULL AND \"distinct_value_count\" >= 2 AND \"distinct_value_count\" <= 200)");
                    table.CheckConstraint("ck_multi_integer_part_position", "\"position\" >= 0 AND \"position\" < 256");
                    table.ForeignKey(
                        name: "FK_catalog_recognition_multi_integer_draft_parts_catalog_recog~",
                        column: x => x.draft_id,
                        principalTable: "catalog_recognition_multi_integer_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_multi_integer_evidence_example",
                table: "catalog_recognition_multi_integer_draft_evidence",
                column: "training_example_id");

            migrationBuilder.CreateIndex(
                name: "ix_multi_integer_part_characteristic",
                table: "catalog_recognition_multi_integer_draft_parts",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_multi_integer_part_characteristic",
                table: "catalog_recognition_multi_integer_draft_parts",
                columns: new[] { "draft_id", "characteristic_definition_id" },
                unique: true,
                filter: "\"characteristic_definition_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_multi_integer_draft_scope",
                table: "catalog_recognition_multi_integer_drafts",
                columns: new[] { "manufacturer_id", "product_type_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_multi_integer_draft_evidence");

            migrationBuilder.DropTable(
                name: "catalog_recognition_multi_integer_draft_parts");

            migrationBuilder.DropTable(
                name: "catalog_recognition_multi_integer_drafts");
        }
    }
}
