using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSelectedComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_selected_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    main_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    need_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_selected_components", x => x.id);
                    table.CheckConstraint("ck_product_selected_components_quantity", "\"quantity\" >= 1 AND \"quantity\" <= 1000000");
                    table.ForeignKey(
                        name: "FK_product_selected_components_component_need_definitions_need~",
                        column: x => x.need_definition_id,
                        principalTable: "component_need_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_selected_components_products_component_product_id",
                        column: x => x.component_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_selected_components_products_main_product_id",
                        column: x => x.main_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_selected_components_component_product_id",
                table: "product_selected_components",
                column: "component_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_selected_components_main_product_id_need_definition~",
                table: "product_selected_components",
                columns: new[] { "main_product_id", "need_definition_id", "component_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_selected_components_need_definition_id",
                table: "product_selected_components",
                column: "need_definition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_selected_components");
        }
    }
}
