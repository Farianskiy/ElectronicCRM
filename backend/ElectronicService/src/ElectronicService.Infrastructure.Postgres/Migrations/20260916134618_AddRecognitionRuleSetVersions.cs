using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionRuleSetVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_rule_set_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_rule_set_versions", x => x.id);
                    table.CheckConstraint("ck_rule_set_version_name", "char_length(btrim(\"name\")) > 0");
                    table.CheckConstraint("ck_rule_set_version_number", "\"version_number\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "catalog_recognition_rule_set_entries",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    literal_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    integer_draft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    multi_integer_draft_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_rule_set_entries", x => new { x.version_id, x.position });
                    table.CheckConstraint("ck_rule_set_entry_kind", "(\r\n    \"kind\" = 1\r\n    AND \"literal_draft_id\" IS NOT NULL\r\n    AND \"integer_draft_id\" IS NULL\r\n    AND \"multi_integer_draft_id\" IS NULL\r\n)\r\nOR\r\n(\r\n    \"kind\" = 2\r\n    AND \"literal_draft_id\" IS NULL\r\n    AND \"integer_draft_id\" IS NOT NULL\r\n    AND \"multi_integer_draft_id\" IS NULL\r\n)\r\nOR\r\n(\r\n    \"kind\" = 3\r\n    AND \"literal_draft_id\" IS NULL\r\n    AND \"integer_draft_id\" IS NULL\r\n    AND \"multi_integer_draft_id\" IS NOT NULL\r\n)");
                    table.CheckConstraint("ck_rule_set_entry_position", "\"position\" >= 0 AND \"position\" < 100");
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_entries_catalog_recognition_in~",
                        column: x => x.integer_draft_id,
                        principalTable: "catalog_recognition_integer_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_entries_catalog_recognition_li~",
                        column: x => x.literal_draft_id,
                        principalTable: "catalog_recognition_literal_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_entries_catalog_recognition_mu~",
                        column: x => x.multi_integer_draft_id,
                        principalTable: "catalog_recognition_multi_integer_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_entries_catalog_recognition_ru~",
                        column: x => x.version_id,
                        principalTable: "catalog_recognition_rule_set_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_entry_integer",
                table: "catalog_recognition_rule_set_entries",
                column: "integer_draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_entry_literal",
                table: "catalog_recognition_rule_set_entries",
                column: "literal_draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_entry_multi_integer",
                table: "catalog_recognition_rule_set_entries",
                column: "multi_integer_draft_id");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_entry_integer",
                table: "catalog_recognition_rule_set_entries",
                columns: new[] { "version_id", "integer_draft_id" },
                unique: true,
                filter: "\"integer_draft_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_entry_literal",
                table: "catalog_recognition_rule_set_entries",
                columns: new[] { "version_id", "literal_draft_id" },
                unique: true,
                filter: "\"literal_draft_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_entry_multi_integer",
                table: "catalog_recognition_rule_set_entries",
                columns: new[] { "version_id", "multi_integer_draft_id" },
                unique: true,
                filter: "\"multi_integer_draft_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_version_scope_number",
                table: "catalog_recognition_rule_set_versions",
                columns: new[] { "manufacturer_id", "product_type_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_rule_set_entries");

            migrationBuilder.DropTable(
                name: "catalog_recognition_rule_set_versions");
        }
    }
}
