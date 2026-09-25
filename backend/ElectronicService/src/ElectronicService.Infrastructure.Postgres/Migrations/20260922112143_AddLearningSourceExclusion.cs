using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningSourceExclusion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_candidates_counts",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_candidates_distinct_products",
                table: "catalog_recognition_candidates");

            migrationBuilder.AddColumn<DateTime>(
                name: "excluded_at_utc",
                table: "catalog_recognition_feedback",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "excluded_by_user_id",
                table: "catalog_recognition_feedback",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exclusion_reason",
                table: "catalog_recognition_feedback",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "evidence_revision",
                table: "catalog_recognition_candidates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "ck_feedback_exclusion",
                table: "catalog_recognition_feedback",
                sql: "(excluded_at_utc IS NULL AND excluded_by_user_id IS NULL AND exclusion_reason IS NULL) OR (excluded_at_utc IS NOT NULL AND excluded_by_user_id IS NOT NULL AND exclusion_reason IS NOT NULL AND char_length(btrim(exclusion_reason)) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_candidates_counts",
                table: "catalog_recognition_candidates",
                sql: "\"occurrence_count\" >= 0 AND \"accepted_count\" >= 0 AND \"corrected_count\" >= 0 AND \"rejected_count\" >= 0 AND \"occurrence_count\" = \"accepted_count\" + \"corrected_count\" + \"rejected_count\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_candidates_distinct_products",
                table: "catalog_recognition_candidates",
                sql: "\"distinct_product_count\" >= 0 AND \"distinct_product_count\" <= \"occurrence_count\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_feedback_exclusion",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_candidates_counts",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_candidates_distinct_products",
                table: "catalog_recognition_candidates");

            migrationBuilder.DropColumn(
                name: "excluded_at_utc",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "excluded_by_user_id",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "exclusion_reason",
                table: "catalog_recognition_feedback");

            migrationBuilder.DropColumn(
                name: "evidence_revision",
                table: "catalog_recognition_candidates");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_candidates_counts",
                table: "catalog_recognition_candidates",
                sql: "\"occurrence_count\" >= 1 AND \"accepted_count\" >= 0 AND \"corrected_count\" >= 0 AND \"rejected_count\" >= 0 AND \"occurrence_count\" = \"accepted_count\" + \"corrected_count\" + \"rejected_count\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_candidates_distinct_products",
                table: "catalog_recognition_candidates",
                sql: "\"distinct_product_count\" >= 1 AND \"distinct_product_count\" <= \"occurrence_count\"");
        }
    }
}
