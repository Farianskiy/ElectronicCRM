using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionRuleSetReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_recognition_rule_set_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_set_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_version = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    evaluator_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    snapshot_format_version = table.Column<int>(type: "integer", nullable: false),
                    total_rows_count = table.Column<int>(type: "integer", nullable: false),
                    proposed_rows_count = table.Column<int>(type: "integer", nullable: false),
                    conflict_rows_count = table.Column<int>(type: "integer", nullable: false),
                    no_match_rows_count = table.Column<int>(type: "integer", nullable: false),
                    outside_scope_rows_count = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_recognition_rule_set_reports", x => x.id);
                    table.CheckConstraint("ck_rule_set_report_batch_version", "\"batch_version\" BETWEEN 0 AND 4294967295");
                    table.CheckConstraint("ck_rule_set_report_counts", "\"total_rows_count\" > 0\r\nAND \"proposed_rows_count\" >= 0\r\nAND \"conflict_rows_count\" >= 0\r\nAND \"no_match_rows_count\" >= 0\r\nAND \"outside_scope_rows_count\" >= 0\r\nAND \"total_rows_count\"::bigint =\r\n    \"proposed_rows_count\"::bigint +\r\n    \"conflict_rows_count\"::bigint +\r\n    \"no_match_rows_count\"::bigint +\r\n    \"outside_scope_rows_count\"::bigint");
                    table.CheckConstraint("ck_rule_set_report_format", "\"snapshot_format_version\" > 0");
                    table.CheckConstraint("ck_rule_set_report_snapshot", "jsonb_typeof(\"snapshot_json\") = 'array'");
                    table.CheckConstraint("ck_rule_set_report_time", "\"completed_at_utc\" >= \"started_at_utc\"");
                    table.ForeignKey(
                        name: "FK_catalog_recognition_rule_set_reports_catalog_recognition_ru~",
                        column: x => x.rule_set_version_id,
                        principalTable: "catalog_recognition_rule_set_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_report_version_batch_time",
                table: "catalog_recognition_rule_set_reports",
                columns: new[] { "rule_set_version_id", "batch_id", "completed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_recognition_rule_set_reports");
        }
    }
}
