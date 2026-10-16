using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentCompatibilityConditionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_component_compatibility_constraints_component_offers_compon~",
                table: "component_compatibility_constraints");

            migrationBuilder.RenameColumn(
                name: "component_offer_id",
                table: "component_compatibility_constraints",
                newName: "condition_group_id");

            migrationBuilder.RenameIndex(
                name: "IX_component_compatibility_constraints_component_offer_id",
                table: "component_compatibility_constraints",
                newName: "IX_component_compatibility_constraints_condition_group_id");

            migrationBuilder.CreateTable(
                name: "component_compatibility_condition_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_compatibility_condition_groups", x => x.id);
                    table.CheckConstraint("ck_component_condition_groups_position_non_negative", "\"position\" >= 0");
                    table.ForeignKey(
                        name: "FK_component_compatibility_condition_groups_component_offers_c~",
                        column: x => x.component_offer_id,
                        principalTable: "component_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO component_compatibility_condition_groups
                    (id, component_offer_id, position)
                SELECT id, id, 0
                FROM component_offers;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_component_compatibility_condition_groups_component_offer_id~",
                table: "component_compatibility_condition_groups",
                columns: new[] { "component_offer_id", "position" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_component_compatibility_constraints_component_compatibility~",
                table: "component_compatibility_constraints",
                column: "condition_group_id",
                principalTable: "component_compatibility_condition_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_component_compatibility_constraints_component_compatibility~",
                table: "component_compatibility_constraints");

            migrationBuilder.Sql(
                """
                UPDATE component_compatibility_constraints AS c
                SET condition_group_id = g.component_offer_id
                FROM component_compatibility_condition_groups AS g
                WHERE c.condition_group_id = g.id;
                """);

            migrationBuilder.DropTable(
                name: "component_compatibility_condition_groups");

            migrationBuilder.RenameColumn(
                name: "condition_group_id",
                table: "component_compatibility_constraints",
                newName: "component_offer_id");

            migrationBuilder.RenameIndex(
                name: "IX_component_compatibility_constraints_condition_group_id",
                table: "component_compatibility_constraints",
                newName: "IX_component_compatibility_constraints_component_offer_id");

            migrationBuilder.AddForeignKey(
                name: "FK_component_compatibility_constraints_component_offers_compon~",
                table: "component_compatibility_constraints",
                column: "component_offer_id",
                principalTable: "component_offers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
