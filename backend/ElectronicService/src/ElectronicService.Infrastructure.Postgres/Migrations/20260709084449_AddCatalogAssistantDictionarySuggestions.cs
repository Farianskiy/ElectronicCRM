using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAssistantDictionarySuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_assistant_dictionary_suggestions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    unknown_phrase = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    normalized_unknown_phrase = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    suggested_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    suggested_target_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    suggested_target_value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_assistant_dictionary_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_assistant_dictionary_suggestions_users_created_by_u~",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_assistant_dictionary_suggestions_users_reviewed_by_~",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assistant_dictionary_suggestions_created_at_utc",
                table: "catalog_assistant_dictionary_suggestions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assistant_dictionary_suggestions_created_by_user_id",
                table: "catalog_assistant_dictionary_suggestions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assistant_dictionary_suggestions_normalized_unknown_phrase",
                table: "catalog_assistant_dictionary_suggestions",
                column: "normalized_unknown_phrase");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assistant_dictionary_suggestions_reviewed_by_user_id",
                table: "catalog_assistant_dictionary_suggestions",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_assistant_dictionary_suggestions_status",
                table: "catalog_assistant_dictionary_suggestions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_assistant_dictionary_suggestions");
        }
    }
}
