using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDictionarySuggestionApprovalDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "approved_characteristic_definition_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_kind",
                table: "catalog_assistant_dictionary_suggestions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_phrase",
                table: "catalog_assistant_dictionary_suggestions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approved_priority",
                table: "catalog_assistant_dictionary_suggestions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_product_type_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_target_code",
                table: "catalog_assistant_dictionary_suggestions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_target_value",
                table: "catalog_assistant_dictionary_suggestions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_dictionary_term_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_approved_character~",
                table: "catalog_assistant_dictionary_suggestions",
                column: "approved_characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions",
                columns: new[] { "approved_product_type_id", "approved_characteristic_definition_id" });

            migrationBuilder.CreateIndex(
                name: "ux_dictionary_suggestions_created_term",
                table: "catalog_assistant_dictionary_suggestions",
                column: "created_dictionary_term_id",
                unique: true,
                filter: "\"created_dictionary_term_id\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_characteristic_scope",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"approved_characteristic_definition_id\" IS NULL OR (\"approved_product_type_id\" IS NOT NULL AND \"approved_kind\" = 'Characteristic')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "(\"approved_phrase\" IS NULL AND \"approved_kind\" IS NULL AND \"approved_target_code\" IS NULL AND \"approved_target_value\" IS NULL AND \"approved_product_type_id\" IS NULL AND \"approved_characteristic_definition_id\" IS NULL AND \"approved_priority\" IS NULL AND \"created_dictionary_term_id\" IS NULL) OR (\"approved_phrase\" IS NOT NULL AND \"approved_kind\" IS NOT NULL AND \"approved_target_value\" IS NOT NULL AND \"approved_priority\" IS NOT NULL AND \"created_dictionary_term_id\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_kind",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"approved_kind\" IS NULL OR \"approved_kind\" IN ('Manufacturer', 'ProductType', 'Characteristic', 'SearchToken')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dictionary_suggestions_approved_priority",
                table: "catalog_assistant_dictionary_suggestions",
                sql: "\"approved_priority\" IS NULL OR (\"approved_priority\" >= 1 AND \"approved_priority\" <= 10000)");

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_approved_characteristic",
                table: "catalog_assistant_dictionary_suggestions",
                column: "approved_characteristic_definition_id",
                principalTable: "characteristic_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_approved_product_type",
                table: "catalog_assistant_dictionary_suggestions",
                column: "approved_product_type_id",
                principalTable: "product_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dictionary_suggestions_created_term",
                table: "catalog_assistant_dictionary_suggestions",
                column: "created_dictionary_term_id",
                principalTable: "catalog_dictionary_terms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_approved_characteristic",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_approved_product_type",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropForeignKey(
                name: "fk_dictionary_suggestions_created_term",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_approved_character~",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ix_dictionary_suggestions_approved_scope",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropIndex(
                name: "ux_dictionary_suggestions_created_term",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_characteristic_scope",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_decision",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_kind",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dictionary_suggestions_approved_priority",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_characteristic_definition_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_kind",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_phrase",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_priority",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_product_type_id",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_target_code",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "approved_target_value",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "created_dictionary_term_id",
                table: "catalog_assistant_dictionary_suggestions");
        }
    }
}
