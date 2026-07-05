using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characteristic_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    data_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characteristic_definitions", x => x.id);
                    table.CheckConstraint("ck_characteristic_definitions_data_type_not_none", "\"data_type\" <> 'None'");
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_type_characteristics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_filterable = table.Column<bool>(type: "boolean", nullable: false),
                    is_used_for_replacement = table.Column<bool>(type: "boolean", nullable: false),
                    replacement_match_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    replacement_weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_type_characteristics", x => x.id);
                    table.CheckConstraint("ck_product_type_characteristics_replacement_match_mode_not_non~", "\"is_used_for_replacement\" = false OR \"replacement_match_mode\" <> 'None'");
                    table.CheckConstraint("ck_product_type_characteristics_replacement_weight_not_negative", "\"replacement_weight\" >= 0");
                    table.ForeignKey(
                        name: "FK_product_type_characteristics_characteristic_definitions_cha~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_type_characteristics_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    stock_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_price_amount_not_negative", "\"price_amount\" >= 0");
                    table.CheckConstraint("ck_products_stock_quantity_not_negative", "\"stock_quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_products_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_product_types_product_type_id",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_aliases_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_characteristic_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value_data_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    value_number = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    value_boolean = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_characteristic_values", x => x.id);
                    table.CheckConstraint("ck_product_characteristic_values_only_one_value_type", "(\r\n    \"value_data_type\" = 'Text'\r\n    AND \"value_text\" IS NOT NULL\r\n    AND \"value_number\" IS NULL\r\n    AND \"value_boolean\" IS NULL\r\n)\r\nOR\r\n(\r\n    \"value_data_type\" = 'Number'\r\n    AND \"value_text\" IS NULL\r\n    AND \"value_number\" IS NOT NULL\r\n    AND \"value_boolean\" IS NULL\r\n)\r\nOR\r\n(\r\n    \"value_data_type\" = 'Boolean'\r\n    AND \"value_text\" IS NULL\r\n    AND \"value_number\" IS NULL\r\n    AND \"value_boolean\" IS NOT NULL\r\n)");
                    table.CheckConstraint("ck_product_characteristic_values_value_data_type_not_none", "\"value_data_type\" <> 'None'");
                    table.ForeignKey(
                        name: "FK_product_characteristic_values_characteristic_definitions_ch~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_characteristic_values_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_characteristic_definitions_code",
                table: "characteristic_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_characteristic_definitions_name",
                table: "characteristic_definitions",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_normalized_name",
                table: "manufacturers",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_aliases_normalized_value",
                table: "product_aliases",
                column: "normalized_value");

            migrationBuilder.CreateIndex(
                name: "IX_product_aliases_product_id",
                table: "product_aliases",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_aliases_product_id_normalized_value",
                table: "product_aliases",
                columns: new[] { "product_id", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_characteristic_values_characteristic_definition_id",
                table: "product_characteristic_values",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_characteristic_values_product_id_characteristic_def~",
                table: "product_characteristic_values",
                columns: new[] { "product_id", "characteristic_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_type_characteristics_characteristic_definition_id",
                table: "product_type_characteristics",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_type_characteristics_product_type_id_characteristic~",
                table: "product_type_characteristics",
                columns: new[] { "product_type_id", "characteristic_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_types_code",
                table: "product_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_types_name",
                table: "product_types",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_products_manufacturer_id",
                table: "products",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_product_type_id",
                table: "products",
                column: "product_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_aliases");

            migrationBuilder.DropTable(
                name: "product_characteristic_values");

            migrationBuilder.DropTable(
                name: "product_type_characteristics");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "characteristic_definitions");

            migrationBuilder.DropTable(
                name: "manufacturers");

            migrationBuilder.DropTable(
                name: "product_types");
        }
    }
}
