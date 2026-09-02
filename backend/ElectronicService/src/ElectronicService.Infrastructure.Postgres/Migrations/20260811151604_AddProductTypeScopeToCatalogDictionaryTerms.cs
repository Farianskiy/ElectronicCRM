using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeScopeToCatalogDictionaryTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_mapping",
                table: "catalog_dictionary_terms");

            migrationBuilder.AddColumn<Guid>(
                name: "product_type_id",
                table: "catalog_dictionary_terms",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_dictionary_terms_product_types_product_type_id",
                table: "catalog_dictionary_terms",
                column: "product_type_id",
                principalTable: "product_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_dictionary_terms_product_types_product_type_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_scope_status",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ux_catalog_dictionary_terms_scope_mapping",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "product_type_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_mapping",
                table: "catalog_dictionary_terms",
                columns: new[] { "normalized_phrase", "kind", "target_code", "target_value" });
        }
    }
}
