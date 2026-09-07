using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPriceCalculations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_price_calculations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(22,2)", precision: 22, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_calculations", x => x.id);
                    table.CheckConstraint("ck_catalog_price_calculations_currency", "char_length(\"currency\") = 3");
                    table.CheckConstraint("ck_catalog_price_calculations_status", "\"status\" <> 'None'");
                    table.CheckConstraint("ck_catalog_price_calculations_total", "\"total_amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_catalog_price_calculations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_price_calculation_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_row_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    manufacturer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    base_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    mrc_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    project_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(22,2)", precision: 22, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_calculation_lines", x => x.id);
                    table.CheckConstraint("ck_catalog_price_calculation_lines_base_price", "\"base_price_amount\" >= 0 AND \"base_price_amount\" <= 1000000000000");
                    table.CheckConstraint("ck_catalog_price_calculation_lines_discount", "\"discount_percent\" >= 0 AND \"discount_percent\" <= 100");
                    table.CheckConstraint("ck_catalog_price_calculation_lines_mrc_price", "\"mrc_price_amount\" IS NULL OR (\"mrc_price_amount\" >= 0 AND \"mrc_price_amount\" <= 1000000000000)");
                    table.CheckConstraint("ck_catalog_price_calculation_lines_project_price", "\"project_price_amount\" >= 0 AND \"project_price_amount\" <= \"base_price_amount\"");
                    table.CheckConstraint("ck_catalog_price_calculation_lines_quantity", "\"quantity\" > 0 AND \"quantity\" <= 1000000");
                    table.CheckConstraint("ck_catalog_price_calculation_lines_total", "\"total_amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_lines_catalog_price_calculations_~",
                        column: x => x.calculation_id,
                        principalTable: "catalog_price_calculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_lines_catalog_price_list_rows_pri~",
                        column: x => x.price_list_row_id,
                        principalTable: "catalog_price_list_rows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_lines_catalog_price_lists_price_l~",
                        column: x => x.price_list_id,
                        principalTable: "catalog_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_lines_manufacturers_manufacturer_~",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_price_calculation_manufacturer_discounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_calculation_manufacturer_discounts", x => x.id);
                    table.CheckConstraint("ck_catalog_price_calculation_discounts_percent", "\"discount_percent\" >= 0 AND \"discount_percent\" <= 100");
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_manufacturer_discounts_catalog_pr~",
                        column: x => x.calculation_id,
                        principalTable: "catalog_price_calculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_manufacturer_discounts_manufactur~",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_lines_calculation_manufacturer",
                table: "catalog_price_calculation_lines",
                columns: new[] { "calculation_id", "manufacturer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_lines_calculation_product",
                table: "catalog_price_calculation_lines",
                columns: new[] { "calculation_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_price_calculation_lines_manufacturer_id",
                table: "catalog_price_calculation_lines",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_lines_price_list",
                table: "catalog_price_calculation_lines",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_lines_price_list_row",
                table: "catalog_price_calculation_lines",
                column: "price_list_row_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_price_calculation_lines_product_id",
                table: "catalog_price_calculation_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_calculation_lines_calculation_price_row",
                table: "catalog_price_calculation_lines",
                columns: new[] { "calculation_id", "price_list_row_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_discounts_manufacturer",
                table: "catalog_price_calculation_manufacturer_discounts",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_calculation_discounts_calculation_manufacturer",
                table: "catalog_price_calculation_manufacturer_discounts",
                columns: new[] { "calculation_id", "manufacturer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculations_user_created",
                table: "catalog_price_calculations",
                columns: new[] { "created_by_user_id", "created_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculations_user_status_created",
                table: "catalog_price_calculations",
                columns: new[] { "created_by_user_id", "status", "created_at_utc" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_price_calculation_lines");

            migrationBuilder.DropTable(
                name: "catalog_price_calculation_manufacturer_discounts");

            migrationBuilder.DropTable(
                name: "catalog_price_calculations");
        }
    }
}
