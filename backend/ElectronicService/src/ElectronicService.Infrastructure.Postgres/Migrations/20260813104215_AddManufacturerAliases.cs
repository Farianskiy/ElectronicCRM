using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manufacturer_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_phrase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturer_aliases", x => x.id);
                    table.CheckConstraint("ck_manufacturer_aliases_approved_after_created", "\"approved_at_utc\" IS NULL OR \"approved_at_utc\" >= \"created_at_utc\"");
                    table.CheckConstraint("ck_manufacturer_aliases_rejected_after_created", "\"rejected_at_utc\" IS NULL OR \"rejected_at_utc\" >= \"created_at_utc\"");
                    table.CheckConstraint("ck_manufacturer_aliases_source", "\"source\" IN ('Seed', 'Import', 'TechnicalUser', 'UserCorrection', 'RecognitionLearning')");
                    table.CheckConstraint("ck_manufacturer_aliases_status", "\"status\" IN ('Pending', 'Approved', 'Rejected')");
                    table.CheckConstraint("ck_manufacturer_aliases_status_dates", "(\"status\" = 'Pending' AND \"approved_at_utc\" IS NULL AND \"rejected_at_utc\" IS NULL) OR (\"status\" = 'Approved' AND \"approved_at_utc\" IS NOT NULL AND \"rejected_at_utc\" IS NULL) OR (\"status\" = 'Rejected' AND \"approved_at_utc\" IS NULL AND \"rejected_at_utc\" IS NOT NULL)");
                    table.CheckConstraint("ck_manufacturer_aliases_updated_after_created", "\"updated_at_utc\" >= \"created_at_utc\"");
                    table.ForeignKey(
                        name: "FK_manufacturer_aliases_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturer_aliases_manufacturer_status",
                table: "manufacturer_aliases",
                columns: new[] { "manufacturer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturer_aliases_status",
                table: "manufacturer_aliases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_manufacturer_aliases_active_normalized_phrase",
                table: "manufacturer_aliases",
                column: "normalized_phrase",
                unique: true,
                filter: "\"status\" IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manufacturer_aliases");
        }
    }
}
