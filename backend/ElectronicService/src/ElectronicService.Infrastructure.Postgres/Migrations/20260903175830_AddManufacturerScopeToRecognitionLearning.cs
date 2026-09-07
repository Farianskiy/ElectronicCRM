using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerScopeToRecognitionLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_recognition_feedback_candidate_scope",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropIndex(
                name: "ix_recognition_candidates_scope_status",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_scope_status",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ux_catalog_dictionary_terms_scope_mapping",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.AddColumn<Guid>(
                name: "manufacturer_id",
                table: "catalog_recognition_feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "manufacturer_id",
                table: "catalog_recognition_candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "manufacturer_id",
                table: "catalog_dictionary_terms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_manufacturer_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "manufacturer_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_candidate_scope",
                table: "catalog_recognition_feedback",
                columns: new[] { "manufacturer_id", "product_type_id", "characteristic_definition_id", "finalized_at_utc" },
                filter: "\"status\" = 'Finalized'");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_feedback_product_type_id",
                table: "catalog_recognition_feedback",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_candidates_product_type_id",
                table: "catalog_recognition_candidates",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_recognition_candidates_scope_status",
                table: "catalog_recognition_candidates",
                columns: new[] { "manufacturer_id", "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_dictionary_terms_product_type_id",
                table: "catalog_dictionary_terms",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_scope_status",
                table: "catalog_dictionary_terms",
                columns: new[] { "manufacturer_id", "product_type_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_dictionary_terms_scope_mapping",
                table: "catalog_dictionary_terms",
                columns: new[] { "manufacturer_id", "product_type_id", "normalized_phrase", "kind", "target_code", "target_value" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_approved_product_t~",
                table: "catalog_assistant_dictionary_suggestions",
                column: "approved_product_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_product_type_id",
                table: "catalog_assistant_dictionary_suggestions",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "approved_manufacturer_id", "approved_product_type_id", "approved_characteristic_definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "manufacturer_id", "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "(\"approved_phrase\" IS NULL AND \"approved_kind\" IS NULL AND \"approved_target_code\" IS NULL AND \"approved_target_value\" IS NULL AND \"approved_manufacturer_id\" IS NULL AND \"approved_product_type_id\" IS NULL AND \"approved_characteristic_definition_id\" IS NULL AND \"approved_priority\" IS NULL AND \"created_dictionary_term_id\" IS NULL) OR (\"approved_phrase\" IS NOT NULL AND \"approved_kind\" IS NOT NULL AND \"approved_target_value\" IS NOT NULL AND \"approved_priority\" IS NOT NULL AND \"created_dictionary_term_id\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"source\" <> 'RecognitionLearning' OR (\"generated_automatically\" = TRUE AND \"manufacturer_id\" IS NOT NULL AND \"product_type_id\" IS NOT NULL AND \"characteristic_definition_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_approved_manufacturer",
                table: "catalog_assistant_dictionary_suggestions",
                column: "approved_manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_manufacturer",
                table: "catalog_assistant_dictionary_suggestions",
                column: "manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_dictionary_terms_manufacturer",
                table: "catalog_dictionary_terms",
                column: "manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_recognition_candidates_manufacturer",
                table: "catalog_recognition_candidates",
                column: "manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_recognition_feedback_manufacturers_manufacturer_id",
                table: "catalog_recognition_feedback",
                column: "manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_approved_manufacturer",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_manufacturer",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_dictionary_terms_manufacturer",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_recognition_candidates_manufacturer",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_recognition_feedback_manufacturers_manufacturer_id",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropIndex(
                name: "ix_catalog_recognition_feedback_candidate_scope",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropIndex(
                name: "IX_catalog_recognition_feedback_product_type_id",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropIndex(
                name: "IX_catalog_recognition_candidates_product_type_id",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropIndex(
                name: "ix_recognition_candidates_scope_status",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropIndex(
                name: "IX_catalog_dictionary_terms_product_type_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_scope_status",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ux_catalog_dictionary_terms_scope_mapping",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_approved_product_t~",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_product_type_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "approved_manufacturer_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_recognition_feedback_candidate_scope",
                table: "catalog_recognition_feedback",
                columns: new[] { "product_type_id", "characteristic_definition_id", "finalized_at_utc" },
                filter: "\"status\" = 'Finalized'");

            migrationBuilder.CreateIndex(
                name: "ix_recognition_candidates_scope_status",
                table: "catalog_recognition_candidates",
                columns: new[] { "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_scope_status",
                table: "catalog_dictionary_terms",
                columns: new[] { "product_type_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_dictionary_terms_scope_mapping",
                table: "catalog_dictionary_terms",
                columns: new[] { "product_type_id", "normalized_phrase", "kind", "target_code", "target_value" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "approved_product_type_id", "approved_characteristic_definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "(\"approved_phrase\" IS NULL AND \"approved_kind\" IS NULL AND \"approved_target_code\" IS NULL AND \"approved_target_value\" IS NULL AND \"approved_product_type_id\" IS NULL AND \"approved_characteristic_definition_id\" IS NULL AND \"approved_priority\" IS NULL AND \"created_dictionary_term_id\" IS NULL) OR (\"approved_phrase\" IS NOT NULL AND \"approved_kind\" IS NOT NULL AND \"approved_target_value\" IS NOT NULL AND \"approved_priority\" IS NOT NULL AND \"created_dictionary_term_id\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"source\" <> 'RecognitionLearning' OR (\"generated_automatically\" = TRUE AND \"product_type_id\" IS NOT NULL AND \"characteristic_definition_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");
        }
    }
}
