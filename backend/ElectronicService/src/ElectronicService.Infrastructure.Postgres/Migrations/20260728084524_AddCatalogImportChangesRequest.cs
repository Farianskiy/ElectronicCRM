using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportChangesRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "changes_request_comment",
                table: "catalog_import_batches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "changes_requested_at_utc",
                table: "catalog_import_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "changes_requested_by_user_id",
                table: "catalog_import_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_import_batches_changes_requested_by_user_id",
                table: "catalog_import_batches",
                column: "changes_requested_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_import_batches_users_changes_requested_by_user_id",
                table: "catalog_import_batches",
                column: "changes_requested_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_import_batches_users_changes_requested_by_user_id",
                table: "catalog_import_batches");

            migrationBuilder.DropIndex(
                name: "IX_catalog_import_batches_changes_requested_by_user_id",
                table: "catalog_import_batches");

            migrationBuilder.DropColumn(
                name: "changes_request_comment",
                table: "catalog_import_batches");

            migrationBuilder.DropColumn(
                name: "changes_requested_at_utc",
                table: "catalog_import_batches");

            migrationBuilder.DropColumn(
                name: "changes_requested_by_user_id",
                table: "catalog_import_batches");
        }
    }
}
