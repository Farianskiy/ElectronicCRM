using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_import_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rows_count = table.Column<int>(type: "integer", nullable: false),
                    valid_rows_count = table.Column<int>(type: "integer", nullable: false),
                    error_rows_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_batches", x => x.id);
                    table.CheckConstraint("ck_catalog_import_batches_error_rows_count", "\"error_rows_count\" >= 0");
                    table.CheckConstraint("ck_catalog_import_batches_file_sha256", "char_length(\"file_sha256\") = 64");
                    table.CheckConstraint("ck_catalog_import_batches_file_size", "\"file_size_bytes\" > 0 AND \"file_size_bytes\" <= 10485760");
                    table.CheckConstraint("ck_catalog_import_batches_rows_count", "\"rows_count\" >= 0");
                    table.CheckConstraint("ck_catalog_import_batches_rows_statistics", "\"valid_rows_count\" + \"error_rows_count\" <= \"rows_count\"");
                    table.CheckConstraint("ck_catalog_import_batches_status_not_none", "\"status\" <> 'None'");
                    table.CheckConstraint("ck_catalog_import_batches_valid_rows_count", "\"valid_rows_count\" >= 0");
                    table.ForeignKey(
                        name: "FK_catalog_import_batches_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_import_batches_users_applied_by_user_id",
                        column: x => x.applied_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_import_batches_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_import_batches_users_rejected_by_user_id",
                        column: x => x.rejected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_import_batches_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_import_files",
                columns: table => new
                {
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_import_files", x => x.batch_id);
                    table.CheckConstraint("ck_catalog_import_files_content", "octet_length(\"content\") > 0 AND octet_length(\"content\") <= 10485760");
                    table.ForeignKey(
                        name: "FK_catalog_import_files_catalog_import_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "catalog_import_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_import_batches_applied_by_user_id",
                table: "catalog_import_batches",
                column: "applied_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_batches_creator_date",
                table: "catalog_import_batches",
                columns: new[] { "created_by_user_id", "created_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_batches_file_sha256",
                table: "catalog_import_batches",
                column: "file_sha256");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_batches_product_type",
                table: "catalog_import_batches",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_import_batches_rejected_by_user_id",
                table: "catalog_import_batches",
                column: "rejected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_import_batches_reviewed_by_user_id",
                table: "catalog_import_batches",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_import_batches_status_date",
                table: "catalog_import_batches",
                columns: new[] { "status", "created_at_utc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_import_files");

            migrationBuilder.DropTable(
                name: "catalog_import_batches");
        }
    }
}
