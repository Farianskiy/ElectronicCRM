using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations;

[DbContext(typeof(ElectronicDbContext))]
[Migration("20260911170000_ChangePolesToTextConfiguration")]
public partial class ChangePolesToTextConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE product_characteristic_values AS value
            SET value_data_type = 'Text',
                value_text = CASE value.value_number
                    WHEN 1 THEN '1P'
                    WHEN 2 THEN '2P'
                    WHEN 3 THEN '3P'
                    WHEN 4 THEN '4P'
                    ELSE trim(trailing '.' FROM trim(trailing '0' FROM value.value_number::text)) || 'P'
                END,
                value_number = NULL
            FROM characteristic_definitions AS definition
            WHERE value.characteristic_definition_id = definition.id
              AND definition.code = 'POLES'
              AND value.value_data_type = 'Number';

            UPDATE characteristic_definitions
            SET data_type = 'Text'
            WHERE code = 'POLES';

            UPDATE catalog_dictionary_terms
            SET target_value = CASE target_value
                WHEN '1' THEN '1P'
                WHEN '2' THEN '2P'
                WHEN '3' THEN '3P'
                WHEN '4' THEN '4P'
                ELSE target_value
            END
            WHERE target_code = 'POLES';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE product_characteristic_values AS value
            SET value_data_type = 'Number',
                value_number = CASE value.value_text
                    WHEN '1P' THEN 1
                    WHEN '1P+N' THEN 2
                    WHEN '2P' THEN 2
                    WHEN '3P' THEN 3
                    WHEN '3P+N' THEN 4
                    WHEN '4P' THEN 4
                    ELSE regexp_replace(value.value_text, 'P$', '')::numeric
                END,
                value_text = NULL
            FROM characteristic_definitions AS definition
            WHERE value.characteristic_definition_id = definition.id
              AND definition.code = 'POLES'
              AND value.value_data_type = 'Text'
              AND (value.value_text IN ('1P+N', '3P+N') OR value.value_text ~ '^[0-9]+([.][0-9]+)?P$');

            UPDATE characteristic_definitions
            SET data_type = 'Number'
            WHERE code = 'POLES';

            UPDATE catalog_dictionary_terms
            SET target_value = CASE target_value
                WHEN '1P' THEN '1'
                WHEN '2P' THEN '2'
                WHEN '3P' THEN '3'
                WHEN '4P' THEN '4'
                ELSE target_value
            END
            WHERE target_code = 'POLES';
            """);
    }
}