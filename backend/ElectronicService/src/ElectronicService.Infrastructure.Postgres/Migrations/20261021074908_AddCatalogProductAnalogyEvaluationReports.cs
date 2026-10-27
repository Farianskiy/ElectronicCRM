using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductAnalogyEvaluationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_product_analogy_evaluation_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuration_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    baseline_configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    baseline_configuration_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    candidate_snapshot_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evaluator_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    control_products = table.Column<int>(type: "integer", nullable: false),
                    control_values = table.Column<int>(type: "integer", nullable: false),
                    improvements = table.Column<int>(type: "integer", nullable: false),
                    regressions = table.Column<int>(type: "integer", nullable: false),
                    candidate_correct = table.Column<int>(type: "integer", nullable: false),
                    candidate_incorrect = table.Column<int>(type: "integer", nullable: false),
                    is_ready = table.Column<bool>(type: "boolean", nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_analogy_evaluation_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_evaluation_reports_catalog_product_~",
                        column: x => x.baseline_configuration_id,
                        principalTable: "catalog_product_analogy_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_evaluation_reports_catalog_product~1",
                        column: x => x.configuration_id,
                        principalTable: "catalog_product_analogy_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_evaluation_reports_users_created_by~",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_analogy_evaluation_reports_baseline_configu~",
                table: "catalog_product_analogy_evaluation_reports",
                column: "baseline_configuration_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_analogy_evaluation_reports_configuration_id~",
                table: "catalog_product_analogy_evaluation_reports",
                columns: new[] { "configuration_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_analogy_evaluation_reports_created_by_user_~",
                table: "catalog_product_analogy_evaluation_reports",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_product_analogy_evaluation_reports");
        }
    }
}
