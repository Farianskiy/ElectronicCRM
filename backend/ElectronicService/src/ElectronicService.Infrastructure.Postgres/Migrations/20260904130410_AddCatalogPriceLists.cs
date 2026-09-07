using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    vat_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rows_count = table.Column<int>(type: "integer", nullable: false),
                    valid_rows_count = table.Column<int>(type: "integer", nullable: false),
                    error_rows_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_lists", x => x.id);
                    table.CheckConstraint("ck_catalog_price_lists_currency", "char_length(\"currency\") = 3");
                    table.CheckConstraint("ck_catalog_price_lists_error_rows_count", "\"error_rows_count\" >= 0");
                    table.CheckConstraint("ck_catalog_price_lists_file_sha256", "char_length(\"file_sha256\") = 64");
                    table.CheckConstraint("ck_catalog_price_lists_file_size", "\"file_size_bytes\" > 0 AND \"file_size_bytes\" <= 31457280");
                    table.CheckConstraint("ck_catalog_price_lists_rows_count", "\"rows_count\" >= 0");
                    table.CheckConstraint("ck_catalog_price_lists_rows_statistics", "\"valid_rows_count\" + \"error_rows_count\" <= \"rows_count\"");
                    table.CheckConstraint("ck_catalog_price_lists_status_not_none", "\"status\" <> 'None'");
                    table.CheckConstraint("ck_catalog_price_lists_valid_rows_count", "\"valid_rows_count\" >= 0");
                    table.CheckConstraint("ck_catalog_price_lists_vat_rate", "\"vat_rate_percent\" >= 0 AND \"vat_rate_percent\" <= 100");
                    table.ForeignKey(
                        name: "FK_catalog_price_lists_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_lists_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_price_list_files",
                columns: table => new
                {
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_list_files", x => x.price_list_id);
                    table.CheckConstraint("ck_catalog_price_list_files_content", "octet_length(\"content\") > 0 AND octet_length(\"content\") <= 31457280");
                    table.ForeignKey(
                        name: "FK_catalog_price_list_files_catalog_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "catalog_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_price_list_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    base_price_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    mrc_price_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    product_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    match_confidence_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_list_rows", x => x.id);
                    table.CheckConstraint("ck_catalog_price_list_rows_base_price", "\"base_price_amount\" IS NULL OR \"base_price_amount\" >= 0");
                    table.CheckConstraint("ck_catalog_price_list_rows_match_confidence", "\"match_confidence_percent\" IS NULL OR (\"match_confidence_percent\" >= 0 AND \"match_confidence_percent\" <= 100)");
                    table.CheckConstraint("ck_catalog_price_list_rows_match_product", "(\"match_status\" IN ('MatchedByArticle', 'MatchedByName', 'MatchedManually') AND \"product_id\" IS NOT NULL) OR (\"match_status\" IN ('Pending', 'Ambiguous', 'ProductNotFound') AND \"product_id\" IS NULL)");
                    table.CheckConstraint("ck_catalog_price_list_rows_match_status", "\"match_status\" <> 'None'");
                    table.CheckConstraint("ck_catalog_price_list_rows_mrc_price", "\"mrc_price_amount\" IS NULL OR \"mrc_price_amount\" >= 0");
                    table.CheckConstraint("ck_catalog_price_list_rows_number", "\"row_number\" > 0");
                    table.ForeignKey(
                        name: "FK_catalog_price_list_rows_catalog_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "catalog_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_price_list_rows_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_list_rows_list_article",
                table: "catalog_price_list_rows",
                columns: new[] { "price_list_id", "normalized_article" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_list_rows_list_product",
                table: "catalog_price_list_rows",
                columns: new[] { "price_list_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_list_rows_list_status_number",
                table: "catalog_price_list_rows",
                columns: new[] { "price_list_id", "match_status", "row_number" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_price_list_rows_product_id",
                table: "catalog_price_list_rows",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_list_rows_list_number",
                table: "catalog_price_list_rows",
                columns: new[] { "price_list_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_price_lists_created_by_user_id",
                table: "catalog_price_lists",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_lists_manufacturer_effective_date",
                table: "catalog_price_lists",
                columns: new[] { "manufacturer_id", "effective_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_lists_manufacturer_status",
                table: "catalog_price_lists",
                columns: new[] { "manufacturer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_lists_active_manufacturer",
                table: "catalog_price_lists",
                column: "manufacturer_id",
                unique: true,
                filter: "\"status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_lists_manufacturer_file_sha256",
                table: "catalog_price_lists",
                columns: new[] { "manufacturer_id", "file_sha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_price_list_files");

            migrationBuilder.DropTable(
                name: "catalog_price_list_rows");

            migrationBuilder.DropTable(
                name: "catalog_price_lists");
        }
    }
}
