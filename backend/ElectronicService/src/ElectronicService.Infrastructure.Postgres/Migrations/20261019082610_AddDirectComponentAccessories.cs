using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectComponentAccessories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_component_offers_component_product_id_need_definition_id",
                table: "component_offers");

            migrationBuilder.AlterColumn<Guid>(
                name: "need_definition_id",
                table: "component_offers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "main_product_type_id",
                table: "component_offers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE component_offers AS offer
                SET main_product_type_id = need.main_product_type_id
                FROM component_need_definitions AS need
                WHERE offer.need_definition_id = need.id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "main_product_type_id",
                table: "component_offers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_offers_component_product_id_need_definition_id",
                table: "component_offers",
                columns: new[] { "component_product_id", "need_definition_id" },
                unique: true,
                filter: "\"need_definition_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_component_offers_main_product_type_id_component_product_id",
                table: "component_offers",
                columns: new[] { "main_product_type_id", "component_product_id" },
                unique: true,
                filter: "\"need_definition_id\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_component_offers_product_types_main_product_type_id",
                table: "component_offers",
                column: "main_product_type_id",
                principalTable: "product_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_component_offers_product_types_main_product_type_id",
                table: "component_offers");

            migrationBuilder.DropIndex(
                name: "IX_component_offers_component_product_id_need_definition_id",
                table: "component_offers");

            migrationBuilder.DropIndex(
                name: "IX_component_offers_main_product_type_id_component_product_id",
                table: "component_offers");

            migrationBuilder.Sql(
                "DELETE FROM component_offers WHERE need_definition_id IS NULL;");

            migrationBuilder.DropColumn(
                name: "main_product_type_id",
                table: "component_offers");

            migrationBuilder.AlterColumn<Guid>(
                name: "need_definition_id",
                table: "component_offers",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_offers_component_product_id_need_definition_id",
                table: "component_offers",
                columns: new[] { "component_product_id", "need_definition_id" },
                unique: true);
        }
    }
}
