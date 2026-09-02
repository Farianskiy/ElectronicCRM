using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogCharacteristicRecognitionProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_characteristic_recognition_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    characteristic_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    minimum_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    configuration_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_characteristic_recognition_profiles", x => x.id);
                    table.CheckConstraint("ck_recognition_profiles_configuration_object", "jsonb_typeof(\"configuration_json\") = 'object'");
                    table.CheckConstraint("ck_recognition_profiles_minimum_confidence", "\"minimum_confidence\" > 0 AND \"minimum_confidence\" <= 1");
                    table.CheckConstraint("ck_recognition_profiles_priority", "\"priority\" >= 1 AND \"priority\" <= 10000");
                    table.CheckConstraint("ck_recognition_profiles_strategy_kind", "\"strategy_kind\" IN ('NumericWithUnit', 'PoleCount', 'EnumToken', 'BooleanAlias', 'Dimensions', 'Dictionary')");
                    table.ForeignKey(
                        name: "FK_catalog_characteristic_recognition_profiles_characteristic_~",
                        column: x => x.characteristic_definition_id,
                        principalTable: "characteristic_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_characteristic_recognition_profiles_product_types_p~",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recognition_profiles_characteristic",
                table: "catalog_characteristic_recognition_profiles",
                column: "characteristic_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_recognition_profiles_product_type_active",
                table: "catalog_characteristic_recognition_profiles",
                columns: new[] { "product_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_recognition_profiles_scope_strategy",
                table: "catalog_characteristic_recognition_profiles",
                columns: new[] { "product_type_id", "characteristic_definition_id", "strategy_kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_characteristic_recognition_profiles");
        }
    }
}
