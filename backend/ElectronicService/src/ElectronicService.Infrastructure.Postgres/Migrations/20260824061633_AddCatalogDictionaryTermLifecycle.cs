using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogDictionaryTermLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "disable_reason",
                table: "catalog_dictionary_terms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at_utc",
                table: "catalog_dictionary_terms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "disabled_by_user_id",
                table: "catalog_dictionary_terms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reactivated_at_utc",
                table: "catalog_dictionary_terms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reactivated_by_user_id",
                table: "catalog_dictionary_terms",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_disabled_by_user_id",
                table: "catalog_dictionary_terms",
                column: "disabled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_reactivated_by_user_id",
                table: "catalog_dictionary_terms",
                column: "reactivated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_dictionary_terms_source",
                table: "catalog_dictionary_terms",
                column: "source");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_approved_after_created",
                table: "catalog_dictionary_terms",
                sql: "\"approved_at_utc\" IS NULL OR \"approved_at_utc\" >= \"created_at_utc\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_disable_reason_not_blank",
                table: "catalog_dictionary_terms",
                sql: "\"disable_reason\" IS NULL OR char_length(btrim(\"disable_reason\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_disabled_after_approved",
                table: "catalog_dictionary_terms",
                sql: "\"disabled_at_utc\" IS NULL OR (\"approved_at_utc\" IS NOT NULL AND \"disabled_at_utc\" >= \"approved_at_utc\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_reactivated_after_disabled",
                table: "catalog_dictionary_terms",
                sql: "\"reactivated_at_utc\" IS NULL OR (\"disabled_at_utc\" IS NOT NULL AND \"reactivated_at_utc\" >= \"disabled_at_utc\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_status",
                table: "catalog_dictionary_terms",
                sql: "\"status\" IN ('Pending', 'Approved', 'Rejected', 'Disabled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_dictionary_terms_status_lifecycle",
                table: "catalog_dictionary_terms",
                sql: "(\"status\" IN ('Pending', 'Rejected') AND \"approved_at_utc\" IS NULL AND \"disabled_at_utc\" IS NULL AND \"disabled_by_user_id\" IS NULL AND \"disable_reason\" IS NULL AND \"reactivated_at_utc\" IS NULL AND \"reactivated_by_user_id\" IS NULL) OR (\"status\" = 'Approved' AND \"approved_at_utc\" IS NOT NULL AND ((\"disabled_at_utc\" IS NULL AND \"disabled_by_user_id\" IS NULL AND \"disable_reason\" IS NULL AND \"reactivated_at_utc\" IS NULL AND \"reactivated_by_user_id\" IS NULL) OR (\"disabled_at_utc\" IS NOT NULL AND \"disabled_by_user_id\" IS NOT NULL AND \"disable_reason\" IS NOT NULL AND \"reactivated_at_utc\" IS NOT NULL AND \"reactivated_by_user_id\" IS NOT NULL))) OR (\"status\" = 'Disabled' AND \"approved_at_utc\" IS NOT NULL AND \"disabled_at_utc\" IS NOT NULL AND \"disabled_by_user_id\" IS NOT NULL AND \"disable_reason\" IS NOT NULL AND \"reactivated_at_utc\" IS NULL AND \"reactivated_by_user_id\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_dictionary_terms_users_disabled_by_user_id",
                table: "catalog_dictionary_terms",
                column: "disabled_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_dictionary_terms_users_reactivated_by_user_id",
                table: "catalog_dictionary_terms",
                column: "reactivated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_dictionary_terms_users_disabled_by_user_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_dictionary_terms_users_reactivated_by_user_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_disabled_by_user_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_reactivated_by_user_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropIndex(
                name: "ix_catalog_dictionary_terms_source",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_approved_after_created",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_disable_reason_not_blank",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_disabled_after_approved",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_reactivated_after_disabled",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_status",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_dictionary_terms_status_lifecycle",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "disable_reason",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "disabled_at_utc",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "disabled_by_user_id",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "reactivated_at_utc",
                table: "catalog_dictionary_terms");

            migrationBuilder.DropColumn(
                name: "reactivated_by_user_id",
                table: "catalog_dictionary_terms");
        }
    }
}
