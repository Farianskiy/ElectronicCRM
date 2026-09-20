using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPriceCalculationProjectCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "catalog_price_calculations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "catalog_price_calculations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_name",
                table: "catalog_price_calculations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project_number",
                table: "catalog_price_calculations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsible_name",
                table: "catalog_price_calculations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_until",
                table: "catalog_price_calculations",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment",
                table: "catalog_price_calculations");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "catalog_price_calculations");

            migrationBuilder.DropColumn(
                name: "object_name",
                table: "catalog_price_calculations");

            migrationBuilder.DropColumn(
                name: "project_number",
                table: "catalog_price_calculations");

            migrationBuilder.DropColumn(
                name: "responsible_name",
                table: "catalog_price_calculations");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "catalog_price_calculations");
        }
    }
}
