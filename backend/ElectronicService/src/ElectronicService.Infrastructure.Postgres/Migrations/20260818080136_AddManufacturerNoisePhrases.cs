using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerNoisePhrases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manufacturer_noise_phrases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_phrase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deactivated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturer_noise_phrases", x => x.id);
                    table.CheckConstraint("ck_manufacturer_noise_phrases_activity_dates", "(\"is_active\" = TRUE AND \"deactivated_at_utc\" IS NULL) OR (\"is_active\" = FALSE AND \"deactivated_at_utc\" IS NOT NULL)");
                    table.CheckConstraint("ck_manufacturer_noise_phrases_deactivated_after_created", "\"deactivated_at_utc\" IS NULL OR \"deactivated_at_utc\" >= \"created_at_utc\"");
                    table.CheckConstraint("ck_manufacturer_noise_phrases_normalized_phrase_not_blank", "char_length(btrim(\"normalized_phrase\")) > 0");
                    table.CheckConstraint("ck_manufacturer_noise_phrases_phrase_not_blank", "char_length(btrim(\"phrase\")) > 0");
                    table.CheckConstraint("ck_manufacturer_noise_phrases_reason_not_blank", "\"reason\" IS NULL OR char_length(btrim(\"reason\")) > 0");
                    table.CheckConstraint("ck_manufacturer_noise_phrases_updated_after_created", "\"updated_at_utc\" >= \"created_at_utc\"");
                    table.ForeignKey(
                        name: "FK_manufacturer_noise_phrases_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_manufacturer_noise_phrases_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturer_noise_phrases_created_by_user_id",
                table: "manufacturer_noise_phrases",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturer_noise_phrases_is_active",
                table: "manufacturer_noise_phrases",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturer_noise_phrases_updated_by_user_id",
                table: "manufacturer_noise_phrases",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_manufacturer_noise_phrases_normalized_phrase",
                table: "manufacturer_noise_phrases",
                column: "normalized_phrase",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manufacturer_noise_phrases");
        }
    }
}
