using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogProductAnalogyConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_product_analogy_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settings_json = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    settings_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    evaluated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    superseded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_product_analogy_configurations", x => x.id);
                    table.CheckConstraint("ck_catalog_product_analogy_configuration_version", "\"version_number\" > 0");
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_configurations_manufacturers_manufa~",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_configurations_product_types_produc~",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_product_analogy_configurations_users_created_by_use~",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_analogy_configurations_created_by_user_id",
                table: "catalog_product_analogy_configurations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_product_analogy_configurations_product_type_id",
                table: "catalog_product_analogy_configurations",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_product_analogy_configuration_active_scope",
                table: "catalog_product_analogy_configurations",
                columns: new[] { "manufacturer_id", "product_type_id" },
                unique: true,
                filter: "\"status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_catalog_product_analogy_configuration_scope_version",
                table: "catalog_product_analogy_configurations",
                columns: new[] { "manufacturer_id", "product_type_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_product_analogy_configurations");
        }
    }
}
