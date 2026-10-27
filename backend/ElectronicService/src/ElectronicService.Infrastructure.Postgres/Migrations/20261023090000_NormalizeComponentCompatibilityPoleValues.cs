using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations;

[DbContext(typeof(ElectronicDbContext))]
[Migration("20261023090000_NormalizeComponentCompatibilityPoleValues")]
public sealed class NormalizeComponentCompatibilityPoleValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE component_compatibility_constraints AS compatibility_constraint
            SET expected_text = CASE UPPER(TRIM(compatibility_constraint.expected_text))
                WHEN '1' THEN '1P'
                WHEN '2' THEN '2P'
                WHEN '3' THEN '3P'
                WHEN '4' THEN '4P'
                ELSE compatibility_constraint.expected_text
            END
            FROM characteristic_definitions AS definition
            WHERE compatibility_constraint.characteristic_definition_id = definition.id
              AND UPPER(REPLACE(REPLACE(definition.code, '-', '_'), ' ', '_')) = 'POLES'
              AND compatibility_constraint.expected_data_type = 'Text'
              AND UPPER(TRIM(compatibility_constraint.expected_text)) IN ('1', '2', '3', '4');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Нормализованные значения нельзя безопасно отличить от корректных
        // значений, существовавших до этой миграции.
    }
}
