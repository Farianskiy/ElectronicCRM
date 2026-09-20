using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionRuleSetSwitches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_rule_set_switches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_rule_set_switches", x => x.id);
                    table.CheckConstraint("ck_rule_set_switch_change", "\"previous_version_id\" IS DISTINCT FROM \"new_version_id\"");
                    table.CheckConstraint("ck_rule_set_switch_first", "\"sequence_number\" <> 1 OR \"previous_version_id\" IS NULL");
                    table.CheckConstraint("ck_rule_set_switch_reason", "char_length(btrim(\"reason\")) > 0");
                    table.CheckConstraint("ck_rule_set_switch_report", "(\"new_version_id\" IS NULL AND \"report_id\" IS NULL)\r\nOR\r\n(\"new_version_id\" IS NOT NULL AND \"report_id\" IS NOT NULL)");
                    table.CheckConstraint("ck_rule_set_switch_sequence", "\"sequence_number\" > 0");
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_switches_catalog_recognition_r~",
                        column: x => x.new_version_id,
                        principalTable: "catalog_recognition_rule_set_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_switches_catalog_recognition_~1",
                        column: x => x.previous_version_id,
                        principalTable: "catalog_recognition_rule_set_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_switches_catalog_recognition_~2",
                        column: x => x.report_id,
                        principalTable: "catalog_recognition_rule_set_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_rule_set_switches_new_version_id",
                table: "catalog_recognition_rule_set_switches",
                column: "new_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_rule_set_switches_previous_version_id",
                table: "catalog_recognition_rule_set_switches",
                column: "previous_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_recognition_rule_set_switches_report_id",
                table: "catalog_recognition_rule_set_switches",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_switch_scope_sequence",
                table: "catalog_recognition_rule_set_switches",
                columns: new[] { "manufacturer_id", "product_type_id", "sequence_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_rule_set_switches");
        }
    }
}
