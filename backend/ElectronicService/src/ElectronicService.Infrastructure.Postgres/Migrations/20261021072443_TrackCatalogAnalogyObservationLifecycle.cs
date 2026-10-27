using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TrackCatalogAnalogyObservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "analogy_decision_recorded_at_utc",
                table: "catalog_recognition_feedback",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "analogy_display_count",
                table: "catalog_recognition_feedback",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "analogy_last_shown_at_utc",
                table: "catalog_recognition_feedback",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analogy_last_stale_reason",
                table: "catalog_recognition_feedback",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "analogy_stale_count",
                table: "catalog_recognition_feedback",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analogy_decision_recorded_at_utc",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "analogy_display_count",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "analogy_last_shown_at_utc",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "analogy_last_stale_reason",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "analogy_stale_count",
                table: "catalog_recognition_feedback");
        }
    }
}
