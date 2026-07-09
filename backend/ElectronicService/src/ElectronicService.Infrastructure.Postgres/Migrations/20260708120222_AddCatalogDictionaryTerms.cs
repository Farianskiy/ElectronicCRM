using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogDictionaryTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_dictionary_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    normalized_phrase = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_dictionary_terms", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_mapping",
                table: "catalog_dictionary_terms",
                columns: new[] { "normalized_phrase", "kind", "target_code", "target_value" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_normalized_phrase",
                table: "catalog_dictionary_terms",
                column: "normalized_phrase");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_status",
                table: "catalog_dictionary_terms",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_dictionary_terms");
        }
    }
}
