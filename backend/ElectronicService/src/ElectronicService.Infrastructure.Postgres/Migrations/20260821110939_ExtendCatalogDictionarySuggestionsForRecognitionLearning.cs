using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCatalogDictionarySuggestionsForRecognitionLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "accepted_evidence_count",
                table: "catalog_assistant_dictionary_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "characteristic_definition_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corrected_evidence_count",
                table: "catalog_assistant_dictionary_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "generated_automatically",
                table: "catalog_assistant_dictionary_suggestions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "occurrence_count",
                table: "catalog_assistant_dictionary_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "product_type_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rejected_evidence_count",
                table: "catalog_assistant_dictionary_suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "catalog_assistant_dictionary_suggestions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Assistant");

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_characteristic",
                table: "catalog_assistant_dictionary_suggestions",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "product_type_id", "characteristic_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_source_status_created",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "source", "status", "created_at_utc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_characteristic_scope",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"characteristic_definition_id\" IS NULL OR (\"product_type_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_evidence_counts",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"occurrence_count\" > 0 AND \"accepted_evidence_count\" >= 0 AND \"corrected_evidence_count\" >= 0 AND \"rejected_evidence_count\" >= 0 AND \"accepted_evidence_count\"::bigint + \"corrected_evidence_count\"::bigint + \"rejected_evidence_count\"::bigint <= \"occurrence_count\"::bigint");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_generated_source",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "NOT (\"source\" = 'Assistant' AND \"generated_automatically\" = TRUE)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"source\" <> 'RecognitionLearning' OR (\"generated_automatically\" = TRUE AND \"product_type_id\" IS NOT NULL AND \"characteristic_definition_id\" IS NOT NULL AND \"suggested_kind\" = 'Characteristic')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_source",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"source\" IN ('Assistant', 'ImportRecognition', 'UserCorrection', 'RecognitionLearning', 'MlRecognition')");

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_characteristic",
                table: "catalog_assistant_dictionary_suggestions",
                column: "characteristic_definition_id",
                principalTable: "characteristic_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_product_type",
                table: "catalog_assistant_dictionary_suggestions",
                column: "product_type_id",
                principalTable: "product_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_characteristic",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_product_type",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_characteristic",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_scope_status",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_source_status_created",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_characteristic_scope",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_evidence_counts",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_generated_source",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_recognition_learning",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_source",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "accepted_evidence_count",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "characteristic_definition_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "corrected_evidence_count",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "generated_automatically",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "occurrence_count",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "product_type_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "rejected_evidence_count",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "source",
                table: "catalog_assistant_dictionary_suggestions");
        }
    }
}
