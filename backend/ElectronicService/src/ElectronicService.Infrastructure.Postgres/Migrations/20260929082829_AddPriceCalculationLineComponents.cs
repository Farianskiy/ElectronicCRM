using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCalculationLineComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_price_calculation_line_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculation_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    component_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    manufacturer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity_per_unit = table.Column<int>(type: "integer", nullable: false),
                    base_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    project_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(22,2)", precision: 22, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_price_calculation_line_components", x => x.id);
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_base_price", "\"base_price_amount\" >= 0 AND \"base_price_amount\" <= 1000000000000");
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_discount", "\"discount_percent\" >= 0 AND \"discount_percent\" <= 100");
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_project_price", "\"project_price_amount\" >= 0 AND \"project_price_amount\" <= \"base_price_amount\"");
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_quantity", "\"quantity_per_unit\" >= 1 AND \"quantity_per_unit\" <= 1000000");
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_total", "\"total_amount\" >= 0");
                    table.CheckConstraint("ck_catalog_price_calculation_line_components_total_quantity", "\"total_quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_line_components_catalog_price_cal~",
                        column: x => x.calculation_line_id,
                        principalTable: "catalog_price_calculation_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_line_components_component_need_de~",
                        column: x => x.need_definition_id,
                        principalTable: "component_need_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_line_components_manufacturers_man~",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_price_calculation_line_components_products_componen~",
                        column: x => x.component_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_line_components_line",
                table: "catalog_price_calculation_line_components",
                column: "calculation_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_line_components_manufacturer",
                table: "catalog_price_calculation_line_components",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_line_components_need",
                table: "catalog_price_calculation_line_components",
                column: "need_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_calculation_line_components_product",
                table: "catalog_price_calculation_line_components",
                column: "component_product_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_price_calculation_line_components_line_need_product",
                table: "catalog_price_calculation_line_components",
                columns: new[] { "calculation_line_id", "need_definition_id", "component_product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_price_calculation_line_components");
        }
    }
}
