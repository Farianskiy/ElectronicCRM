using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "component_need_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    main_product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_need_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_component_need_definitions_product_types_main_product_type_~",
                        column: x => x.main_product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "component_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_component_offers_component_need_definitions_need_definition~",
                        column: x => x.need_definition_id,
                        principalTable: "component_need_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_component_offers_products_component_product_id",
                        column: x => x.component_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_component_need_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_component_need_states", x => x.id);
                    table.CheckConstraint("ck_product_component_need_status", "\"status\" IN ('Unknown', 'Missing', 'Included', 'NotApplicable')");
                    table.ForeignKey(
                        name: "FK_product_component_need_states_component_need_definitions_ne~",
                        column: x => x.need_definition_id,
                        principalTable: "component_need_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_component_need_states_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "component_compatibility_constraints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_data_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expected_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    expected_number = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    expected_boolean = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_compatibility_constraints", x => x.id);
                    table.CheckConstraint("ck_component_constraints_data_type_not_none", "\"expected_data_type\" <> 'None'");
                    table.CheckConstraint("ck_component_constraints_only_one_value_type", "(\n    \"expected_data_type\" = 'Text'\n    AND \"expected_text\" IS NOT NULL\n    AND \"expected_number\" IS NULL\n    AND \"expected_boolean\" IS NULL\n)\nOR\n(\n    \"expected_data_type\" = 'Number'\n    AND \"expected_text\" IS NULL\n    AND \"expected_number\" IS NOT NULL\n    AND \"expected_boolean\" IS NULL\n)\nOR\n(\n    \"expected_data_type\" = 'Boolean'\n    AND \"expected_text\" IS NULL\n    AND \"expected_number\" IS NULL\n    AND \"expected_boolean\" IS NOT NULL\n)");
                    table.ForeignKey(
                        name: "FK_component_compatibility_constraints_characteristic_definiti~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_compatibility_constraints_component_offers_compon~",
                        column: x => x.component_offer_id,
                        principalTable: "component_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_component_compatibility_constraints_characteristic_definiti~",
                table: "component_compatibility_constraints",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_compatibility_constraints_component_offer_id",
                table: "component_compatibility_constraints",
                column: "component_offer_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_need_definitions_main_product_type_id_code",
                table: "component_need_definitions",
                columns: new[] { "main_product_type_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_offers_component_product_id_need_definition_id",
                table: "component_offers",
                columns: new[] { "component_product_id", "need_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_offers_need_definition_id",
                table: "component_offers",
                column: "need_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_component_need_states_need_definition_id",
                table: "product_component_need_states",
                column: "need_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_component_need_states_product_id_need_definition_id",
                table: "product_component_need_states",
                columns: new[] { "product_id", "need_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "component_compatibility_constraints");

            migrationBuilder.DropTable(
                name: "product_component_need_states");

            migrationBuilder.DropTable(
                name: "component_offers");

            migrationBuilder.DropTable(
                name: "component_need_definitions");
        }
    }
}
