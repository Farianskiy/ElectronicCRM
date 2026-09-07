using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPriceListRowIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_base_price",
                table: "catalog_price_list_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_mrc_price",
                table: "catalog_price_list_rows");

            migrationBuilder.RenameIndex(
                name: "ix_catalog_price_list_rows_list_status_number",
                table: "catalog_price_list_rows",
                newName: "ix_catalog_price_list_rows_list_match_status_number");

            migrationBuilder.AlterColumn<string>(
                name: "unit",
                table: "catalog_price_list_rows",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "catalog_price_list_rows",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "article",
                table: "catalog_price_list_rows",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "issues_json",
                table: "catalog_price_list_rows",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "catalog_price_list_rows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog_price_list_rows
                SET
                    status = CASE
                        WHEN match_status IN ('MatchedByArticle', 'MatchedByName', 'MatchedManually')
                             AND product_id IS NOT NULL
                             AND base_price_amount IS NOT NULL
                        THEN 'Valid'
                        ELSE 'Error'
                    END,
                    issues_json = CASE
                        WHEN match_status IN ('MatchedByArticle', 'MatchedByName', 'MatchedManually')
                             AND product_id IS NOT NULL
                             AND base_price_amount IS NOT NULL
                        THEN '[]'::jsonb
                        ELSE jsonb_build_array(
                            jsonb_build_object(
                                'code',
                                'legacy_row.requires_reprocessing',
                                'field',
                                'row',
                                'message',
                                'Строка была создана до введения построчной проверки и требует повторной обработки.'))
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "catalog_price_list_rows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_price_list_rows_list_row_status_number",
                table: "catalog_price_list_rows",
                columns: new[] { "price_list_id", "status", "row_number" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_base_price",
                table: "catalog_price_list_rows",
                sql: "\"base_price_amount\" IS NULL OR \"base_price_amount\" >= 0 OR \"status\" = 'Error'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_issues",
                table: "catalog_price_list_rows",
                sql: "(\"status\" = 'Error' AND jsonb_array_length(\"issues_json\") > 0) OR (\"status\" IN ('Pending', 'Valid') AND jsonb_array_length(\"issues_json\") = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_mrc_price",
                table: "catalog_price_list_rows",
                sql: "\"mrc_price_amount\" IS NULL OR \"mrc_price_amount\" >= 0 OR \"status\" = 'Error'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_status",
                table: "catalog_price_list_rows",
                sql: "\"status\" <> 'None'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_price_list_rows_list_row_status_number",
                table: "catalog_price_list_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_base_price",
                table: "catalog_price_list_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_issues",
                table: "catalog_price_list_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_mrc_price",
                table: "catalog_price_list_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_price_list_rows_status",
                table: "catalog_price_list_rows");

            migrationBuilder.DropColumn(
                name: "issues_json",
                table: "catalog_price_list_rows");

            migrationBuilder.DropColumn(
                name: "status",
                table: "catalog_price_list_rows");

            migrationBuilder.RenameIndex(
                name: "ix_catalog_price_list_rows_list_match_status_number",
                table: "catalog_price_list_rows",
                newName: "ix_catalog_price_list_rows_list_status_number");

            migrationBuilder.AlterColumn<string>(
                name: "unit",
                table: "catalog_price_list_rows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "catalog_price_list_rows",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "article",
                table: "catalog_price_list_rows",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_base_price",
                table: "catalog_price_list_rows",
                sql: "\"base_price_amount\" IS NULL OR \"base_price_amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_price_list_rows_mrc_price",
                table: "catalog_price_list_rows",
                sql: "\"mrc_price_amount\" IS NULL OR \"mrc_price_amount\" >= 0");
        }
    }
}
