using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDictionaryEvaluationRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_assistant_dictionary_suggestions FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_recognition_candidates FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                CREATE TRIGGER recognition_dependency_gate BEFORE INSERT OR UPDATE OR DELETE ON catalog_recognition_candidate_evidence FOR EACH STATEMENT EXECUTE FUNCTION recognition_dependency_write_gate();
                """);
            migrationBuilder.AddColumn<Guid>(
                name: "evaluation_report_id",
                table: "catalog_assistant_dictionary_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_dictionary_evaluation_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    suggestion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_dictionary_evaluation_reports", x => x.id);
                    table.CheckConstraint("ck_dictionary_evaluation_snapshot", "jsonb_typeof(snapshot_json) = 'object' AND octet_length(snapshot_json::text) <= 10000000");
                    table.ForeignKey(
                        name: "FK_catalog_dictionary_evaluation_reports_catalog_assistant_dic~",
                        column: x => x.suggestion_id,
                        principalTable: "catalog_assistant_dictionary_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_dictionary_evaluation_reports_catalog_recognition_c~",
                        column: x => x.candidate_id,
                        principalTable: "catalog_recognition_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_evaluation_report_~",
                table: "catalog_assistant_dictionary_suggestions",
                column: "evaluation_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_dictionary_evaluation_reports_candidate_id",
                table: "catalog_dictionary_evaluation_reports",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_dictionary_evaluation_reports_created_by_user_id_su~",
                table: "catalog_dictionary_evaluation_reports",
                columns: new[] { "created_by_user_id", "suggestion_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_dictionary_evaluation_reports_suggestion_id",
                table: "catalog_dictionary_evaluation_reports",
                column: "suggestion_id");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_assistant_dictionary_suggestions_catalog_dictionary~",
                table: "catalog_assistant_dictionary_suggestions",
                column: "evaluation_report_id",
                principalTable: "catalog_dictionary_evaluation_reports",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER recognition_dependency_gate ON catalog_assistant_dictionary_suggestions;
                DROP TRIGGER recognition_dependency_gate ON catalog_recognition_candidates;
                DROP TRIGGER recognition_dependency_gate ON catalog_recognition_candidate_evidence;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_assistant_dictionary_suggestions_catalog_dictionary~",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropTable(
                name: "catalog_dictionary_evaluation_reports");

            migrationBuilder.DropIndex(
                name: "IX_catalog_assistant_dictionary_suggestions_evaluation_report_~",
                table: "catalog_assistant_dictionary_suggestions");

            migrationBuilder.DropColumn(
                name: "evaluation_report_id",
                table: "catalog_assistant_dictionary_suggestions");
        }
    }
}
