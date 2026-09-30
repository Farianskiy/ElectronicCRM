using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SeedVruComponentCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO product_types (id, code, name, kind)
                SELECT source.id, source.code, source.name, 'Component'
                FROM (VALUES
                    ('8f2a1001-7b10-4f61-a001-000000000001'::uuid, 'VRU_SIDE_PANEL', 'Торцевая панель ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000002'::uuid, 'VRU_MOUNTING_PANEL', 'Монтажная панель ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000003'::uuid, 'VRU_MOUNTING_ANGLE', 'Монтажный уголок ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000004'::uuid, 'VRU_CROSS_PROFILE', 'Поперечный профиль ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000005'::uuid, 'VRU_BOTTOM_PLATE_SET', 'Комплект донных пластин ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000006'::uuid, 'VRU_AIR_BREAKER_INSTALLATION_SET', 'Комплект установки воздушного автомата ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000007'::uuid, 'VRU_BUS_BAR_HOLDER_MOUNT_SET', 'Комплект крепления ИШП ВРУ'),
                    ('8f2a1001-7b10-4f61-a001-000000000008'::uuid, 'VRU_PLINTH', 'Цоколь ВРУ')
                ) AS source(id, code, name)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM product_types existing
                    WHERE existing.code = source.code
                );

                INSERT INTO characteristic_definitions (id, code, name, data_type, unit)
                SELECT source.id, source.code, source.name, 'Number', 'мм'
                FROM (VALUES
                    ('8f2a2001-7b10-4f61-a001-000000000001'::uuid, 'LENGTH', 'Длина'),
                    ('8f2a2001-7b10-4f61-a001-000000000002'::uuid, 'THICKNESS', 'Толщина'),
                    ('8f2a2001-7b10-4f61-a001-000000000003'::uuid, 'MOUNTING_SPACING', 'Монтажный размер')
                ) AS source(id, code, name)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM characteristic_definitions existing
                    WHERE existing.code = source.code
                );

                INSERT INTO product_type_characteristics (
                    id,
                    product_type_id,
                    characteristic_definition_id,
                    is_required,
                    is_filterable,
                    is_used_for_replacement,
                    replacement_match_mode,
                    replacement_weight)
                SELECT
                    md5('vru:' || mapping.type_code || ':' || mapping.characteristic_code)::uuid,
                    product_type.id,
                    characteristic.id,
                    false,
                    true,
                    false,
                    'None',
                    0
                FROM (VALUES
                    ('VRU_SIDE_PANEL', 'HEIGHT'),
                    ('VRU_SIDE_PANEL', 'DEPTH'),
                    ('VRU_MOUNTING_PANEL', 'HEIGHT'),
                    ('VRU_MOUNTING_PANEL', 'WIDTH'),
                    ('VRU_MOUNTING_PANEL', 'THICKNESS'),
                    ('VRU_MOUNTING_ANGLE', 'LENGTH'),
                    ('VRU_CROSS_PROFILE', 'LENGTH'),
                    ('VRU_CROSS_PROFILE', 'WIDTH'),
                    ('VRU_BOTTOM_PLATE_SET', 'WIDTH'),
                    ('VRU_BOTTOM_PLATE_SET', 'DEPTH'),
                    ('VRU_AIR_BREAKER_INSTALLATION_SET', 'WIDTH'),
                    ('VRU_BUS_BAR_HOLDER_MOUNT_SET', 'MOUNTING_SPACING'),
                    ('VRU_PLINTH', 'HEIGHT'),
                    ('VRU_PLINTH', 'WIDTH'),
                    ('VRU_PLINTH', 'DEPTH')
                ) AS mapping(type_code, characteristic_code)
                JOIN product_types product_type ON product_type.code = mapping.type_code
                JOIN characteristic_definitions characteristic ON characteristic.code = mapping.characteristic_code
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM product_type_characteristics existing
                    WHERE existing.product_type_id = product_type.id
                      AND existing.characteristic_definition_id = characteristic.id
                );

                INSERT INTO component_need_definitions (
                    id,
                    main_product_type_id,
                    code,
                    name,
                    created_at_utc)
                SELECT
                    source.id,
                    main_product_type.id,
                    source.code,
                    source.name,
                    CURRENT_TIMESTAMP
                FROM (VALUES
                    ('8f2a3001-7b10-4f61-a001-000000000001'::uuid, 'SIDE_PANEL', 'Торцевая панель'),
                    ('8f2a3001-7b10-4f61-a001-000000000002'::uuid, 'MOUNTING_PANEL', 'Монтажная панель'),
                    ('8f2a3001-7b10-4f61-a001-000000000003'::uuid, 'MOUNTING_ANGLE', 'Монтажный уголок'),
                    ('8f2a3001-7b10-4f61-a001-000000000004'::uuid, 'CROSS_PROFILE', 'Поперечный профиль'),
                    ('8f2a3001-7b10-4f61-a001-000000000005'::uuid, 'BOTTOM_PLATES', 'Комплект донных пластин'),
                    ('8f2a3001-7b10-4f61-a001-000000000006'::uuid, 'AIR_BREAKER_INSTALLATION', 'Установка воздушного автомата'),
                    ('8f2a3001-7b10-4f61-a001-000000000007'::uuid, 'BUS_BAR_HOLDER_MOUNT', 'Крепление ИШП'),
                    ('8f2a3001-7b10-4f61-a001-000000000008'::uuid, 'PLINTH', 'Цоколь')
                ) AS source(id, code, name)
                JOIN product_types main_product_type
                  ON main_product_type.code = 'FLOOR_STANDING_CABINET'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM component_need_definitions existing
                    WHERE existing.main_product_type_id = main_product_type.id
                      AND existing.code = source.code
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM component_need_definitions
                WHERE id IN (
                    '8f2a3001-7b10-4f61-a001-000000000001'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000002'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000003'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000004'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000005'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000006'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000007'::uuid,
                    '8f2a3001-7b10-4f61-a001-000000000008'::uuid
                );

                DELETE FROM product_types
                WHERE id IN (
                    '8f2a1001-7b10-4f61-a001-000000000001'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000002'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000003'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000004'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000005'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000006'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000007'::uuid,
                    '8f2a1001-7b10-4f61-a001-000000000008'::uuid
                );

                DELETE FROM characteristic_definitions definition
                WHERE definition.id IN (
                    '8f2a2001-7b10-4f61-a001-000000000001'::uuid,
                    '8f2a2001-7b10-4f61-a001-000000000002'::uuid,
                    '8f2a2001-7b10-4f61-a001-000000000003'::uuid
                )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM product_type_characteristics relation
                      WHERE relation.characteristic_definition_id = definition.id
                  );
                """);
        }
    }
}
