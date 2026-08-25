using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogRecognitionFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    normalized_product_name = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_code_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_code_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    suggested_raw_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    suggested_normalized_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    suggested_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    suggested_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    span_start = table.Column<int>(type: "integer", nullable: true),
                    span_length = table.Column<int>(type: "integer", nullable: true),
                    final_normalized_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    feedback_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label_quality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dictionary_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recognition_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    import_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    import_row_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewer_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finalized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_training_eligible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_feedback", x => x.id);
                    table.CheckConstraint("ck_catalog_recognition_feedback_accepted_value", "\"feedback_type\" <> 'Accepted' OR (\"suggested_normalized_value\" IS NOT NULL AND \"final_normalized_value\" = \"suggested_normalized_value\")");
                    table.CheckConstraint("ck_catalog_recognition_feedback_added_manually", "\"feedback_type\" <> 'AddedManually' OR \"suggested_normalized_value\" IS NULL");
                    table.CheckConstraint("ck_catalog_recognition_feedback_characteristic_code", "char_length(btrim(\"characteristic_code_snapshot\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_confidence", "\"suggested_confidence\" IS NULL OR (\"suggested_confidence\" >= 0 AND \"suggested_confidence\" <= 1)");
                    table.CheckConstraint("ck_catalog_recognition_feedback_confidence_evidence", "\"suggested_confidence\" IS NULL OR \"suggested_normalized_value\" IS NOT NULL");
                    table.CheckConstraint("ck_catalog_recognition_feedback_corrected_value", "\"feedback_type\" <> 'Corrected' OR (\"suggested_normalized_value\" IS NOT NULL AND \"final_normalized_value\" <> \"suggested_normalized_value\")");
                    table.CheckConstraint("ck_catalog_recognition_feedback_final_value", "(\"feedback_type\" IN ('None', 'Rejected') AND \"final_normalized_value\" IS NULL) OR (\"feedback_type\" IN ('Accepted', 'Corrected', 'AddedManually', 'ConflictResolved') AND \"final_normalized_value\" IS NOT NULL AND char_length(btrim(\"final_normalized_value\")) > 0)");
                    table.CheckConstraint("ck_catalog_recognition_feedback_finalized_date", "\"finalized_at_utc\" IS NULL OR \"finalized_at_utc\" >= \"created_at_utc\"");
                    table.CheckConstraint("ck_catalog_recognition_feedback_import_links", "(\"import_batch_id\" IS NULL AND \"import_row_id\" IS NULL) OR (\"import_batch_id\" IS NOT NULL AND \"import_row_id\" IS NOT NULL)");
                    table.CheckConstraint("ck_catalog_recognition_feedback_label_quality", "\"label_quality\" IN ('None', 'Weak', 'Medium', 'Strong')");
                    table.CheckConstraint("ck_catalog_recognition_feedback_lifecycle", "(\"status\" = 'Pending' AND \"feedback_type\" = 'None' AND \"label_quality\" = 'None' AND \"final_normalized_value\" IS NULL AND \"reviewed_by_user_id\" IS NULL AND \"reviewer_role\" IS NULL AND \"finalized_at_utc\" IS NULL AND \"is_training_eligible\" = FALSE) OR (\"status\" = 'Finalized' AND \"feedback_type\" <> 'None' AND \"label_quality\" <> 'None' AND \"reviewed_by_user_id\" IS NOT NULL AND \"reviewer_role\" IS NOT NULL AND \"finalized_at_utc\" IS NOT NULL)");
                    table.CheckConstraint("ck_catalog_recognition_feedback_model_version", "\"model_version\" IS NULL OR char_length(btrim(\"model_version\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_normalized_product_name", "char_length(btrim(\"normalized_product_name\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_product_name", "char_length(btrim(\"product_name\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_product_type_code", "char_length(btrim(\"product_type_code_snapshot\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_reviewer_role", "\"reviewer_role\" IS NULL OR char_length(btrim(\"reviewer_role\")) > 0");
                    table.CheckConstraint("ck_catalog_recognition_feedback_span", "(\"span_start\" IS NULL AND \"span_length\" IS NULL) OR (\"span_start\" IS NOT NULL AND \"span_start\" >= 0 AND \"span_length\" IS NOT NULL AND \"span_length\" > 0 AND \"suggested_raw_value\" IS NOT NULL AND \"span_start\" + \"span_length\" <= char_length(\"product_name\"))");
                    table.CheckConstraint("ck_catalog_recognition_feedback_status", "\"status\" IN ('Pending', 'Finalized')");
                    table.CheckConstraint("ck_catalog_recognition_feedback_suggested_evidence", "(\"suggested_raw_value\" IS NULL AND \"suggested_normalized_value\" IS NULL AND \"suggested_source\" IS NULL) OR (\"suggested_raw_value\" IS NOT NULL AND char_length(btrim(\"suggested_raw_value\")) > 0 AND \"suggested_normalized_value\" IS NOT NULL AND char_length(btrim(\"suggested_normalized_value\")) > 0 AND \"suggested_source\" IS NOT NULL AND char_length(btrim(\"suggested_source\")) > 0)");
                    table.CheckConstraint("ck_catalog_recognition_feedback_training_quality", "\"is_training_eligible\" = FALSE OR (\"status\" = 'Finalized' AND \"label_quality\" IN ('Medium', 'Strong'))");
                    table.CheckConstraint("ck_catalog_recognition_feedback_type", "\"feedback_type\" IN ('None', 'Accepted', 'Corrected', 'Rejected', 'AddedManually', 'ConflictResolved')");
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_catalog_characteristic_recogni~",
                        column: x => x.recognition_profile_id,
                        principalTable: "catalog_characteristic_recognition_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_catalog_dictionary_terms_dicti~",
                        column: x => x.dictionary_term_id,
                        principalTable: "catalog_dictionary_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_catalog_import_batches_import_~",
                        column: x => x.import_batch_id,
                        principalTable: "catalog_import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_catalog_import_rows_import_row~",
                        column: x => x.import_row_id,
                        principalTable: "catalog_import_rows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_characteristic_definitions_cha~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_feedback_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_candidate_scope",
                table: "catalog_recognition_feedback",
                columns: new[] { "product_type_id", "characteristic_definition_id", "finalized_at_utc" },
                filter: "\"status\" = 'Finalized'");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_feedback_characteristic_definition_id",
                table: "catalog_recognition_feedback",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_dictionary_term",
                table: "catalog_recognition_feedback",
                column: "dictionary_term_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_import_source",
                table: "catalog_recognition_feedback",
                columns: new[] { "import_batch_id", "import_row_id" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_recognition_profile",
                table: "catalog_recognition_feedback",
                column: "recognition_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_reviewer",
                table: "catalog_recognition_feedback",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_training_export",
                table: "catalog_recognition_feedback",
                column: "finalized_at_utc",
                filter: "\"status\" = 'Finalized' AND \"is_training_eligible\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_type_date",
                table: "catalog_recognition_feedback",
                columns: new[] { "feedback_type", "finalized_at_utc" },
                filter: "\"status\" = 'Finalized'");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_recognition_feedback_import_row_characteristic",
                table: "catalog_recognition_feedback",
                columns: new[] { "import_row_id", "characteristic_definition_id" },
                unique: true,
                filter: "\"import_row_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_feedback");
        }
    }
}
