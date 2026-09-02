using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogRecognitionCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    normalized_phrase = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_code_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_code_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    proposed_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    candidate_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    accepted_count = table.Column<int>(type: "integer", nullable: false),
                    corrected_count = table.Column<int>(type: "integer", nullable: false),
                    rejected_count = table.Column<int>(type: "integer", nullable: false),
                    distinct_product_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    suggestion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_candidates", x => x.id);
                    table.CheckConstraint("ck_catalog_recognition_candidates_candidate_key", "\"candidate_key\" = upper(\"candidate_key\") AND char_length(\"candidate_key\") = 64");
                    table.CheckConstraint("ck_catalog_recognition_candidates_characteristic_code", "char_length(btrim(\"characteristic_code_snapshot\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_candidates_counts", "\"occurrence_count\" >= 1 AND \"accepted_count\" >= 0 AND \"corrected_count\" >= 0 AND \"rejected_count\" >= 0 AND \"occurrence_count\" = \"accepted_count\" + \"corrected_count\" + \"rejected_count\"");
                    table.CheckConstraint("ck_catalog_recognition_candidates_dates", "\"last_seen_at_utc\" >= \"first_seen_at_utc\"");
                    table.CheckConstraint("ck_catalog_recognition_candidates_distinct_products", "\"distinct_product_count\" >= 1 AND \"distinct_product_count\" <= \"occurrence_count\"");
                    table.CheckConstraint("ck_catalog_recognition_candidates_normalized_phrase", "char_length(btrim(\"normalized_phrase\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_candidates_phrase", "char_length(btrim(\"phrase\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_candidates_product_type_code", "char_length(btrim(\"product_type_code_snapshot\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_candidates_proposed_value", "char_length(btrim(\"proposed_value\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_candidates_status", "\"status\" IN ('Accumulating', 'SuggestionCreated', 'Approved', 'Rejected')");
                    table.CheckConstraint("ck_catalog_recognition_candidates_suggestion_lifecycle", "(\"status\" = 'Accumulating' AND \"suggestion_id\" IS NULL) OR (\"status\" IN ('SuggestionCreated', 'Approved', 'Rejected') AND \"suggestion_id\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_recognition_candidates_characteristic",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recognition_candidates_product_type",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recognition_candidates_suggestion",
                        column: x => x.suggestion_id,
                        principalTable: "catalog_assistant_dictionary_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_recognition_candidate_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_candidate_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_recognition_candidate_evidence_candidate",
                        column: x => x.candidate_id,
                        principalTable: "catalog_recognition_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recognition_candidate_evidence_feedback",
                        column: x => x.feedback_id,
                        principalTable: "catalog_recognition_feedback",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recognition_candidate_evidence_candidate_date",
                table: "catalog_recognition_candidate_evidence",
                columns: new[] { "candidate_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_recognition_candidate_evidence_feedback",
                table: "catalog_recognition_candidate_evidence",
                column: "feedback_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_candidates_characteristic_definition_id",
                table: "catalog_recognition_candidates",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_recognition_candidates_scope_status",
                table: "catalog_recognition_candidates",
                columns: new[] { "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_recognition_candidates_status_last_seen",
                table: "catalog_recognition_candidates",
                columns: new[] { "status", "last_seen_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_recognition_candidates_candidate_key",
                table: "catalog_recognition_candidates",
                column: "candidate_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_recognition_candidates_suggestion",
                table: "catalog_recognition_candidates",
                column: "suggestion_id",
                unique: true,
                filter: "\"suggestion_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_candidate_evidence");

            migrationBuilder.DropTable(
                name: "catalog_recognition_candidates");
        }
    }
}
