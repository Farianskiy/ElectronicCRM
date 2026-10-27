using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAnalogyEvidenceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "evidence_snapshot_json",
                table: "catalog_recognition_feedback",
                type: "character varying(32000)",
                maxLength: 32000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "evidence_snapshot_json",
                table: "catalog_recognition_feedback");
        }
    }
}
