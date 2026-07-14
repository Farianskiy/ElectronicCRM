\set ON_ERROR_STOP on
\pset pager off

SET search_path TO public;
SET client_min_messages TO notice;

BEGIN;

LOCK TABLE manufacturers IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE products IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE catalog_dictionary_terms IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE cleanup_totals
ON COMMIT DROP
AS
SELECT count(*)::bigint AS products_count
FROM products;

CREATE TEMP TABLE confirmed_manufacturer_aliases (
    phrase text NOT NULL,
    normalized_phrase text PRIMARY KEY,
    canonical_name text NOT NULL,
    canonical_normalized_name text NOT NULL
)
ON COMMIT DROP;

INSERT INTO confirmed_manufacturer_aliases (
    phrase,
    normalized_phrase,
    canonical_name,
    canonical_normalized_name)
VALUES
    ('IEK пром серия', 'IEK ПРОМ СЕРИЯ', 'IEK', 'IEK'),
    ('DEKraft пром серия', 'DEKRAFT ПРОМ СЕРИЯ', 'DEKraft', 'DEKRAFT'),
    ('АВВ пром серия', 'АВВ ПРОМ СЕРИЯ', 'ABB', 'ABB'),
    ('KEAZ', 'KEAZ', 'КЭАЗ', 'КЭАЗ'),
    ('TDM', 'TDM', 'ТДМ', 'ТДМ'),
    ('ДКС', 'ДКС', 'DKC', 'DKC'),
    ('Legrand пром серия', 'LEGRAND ПРОМ СЕРИЯ', 'Legrand', 'LEGRAND'),
    ('LSIS пром серия', 'LSIS ПРОМ СЕРИЯ', 'LSIS', 'LSIS'),
    ('Шнайдер CVS', 'ШНАЙДЕР CVS', 'Schneider Electric', 'SCHNEIDER ELECTRIC'),
    ('Шнайдер EZC', 'ШНАЙДЕР EZC', 'Schneider Electric', 'SCHNEIDER ELECTRIC'),
    ('Шнайдер пром', 'ШНАЙДЕР ПРОМ', 'Schneider Electric', 'SCHNEIDER ELECTRIC');

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms AS existing_term
        JOIN confirmed_manufacturer_aliases AS confirmed_alias
            ON confirmed_alias.normalized_phrase = existing_term.normalized_phrase
        WHERE existing_term.kind = 'Manufacturer'
          AND existing_term.status = 'Approved'
          AND existing_term.target_value <> confirmed_alias.canonical_normalized_name)
    THEN
        RAISE EXCEPTION
            'A confirmed alias conflicts with an existing approved dictionary term. No data was changed.';
    END IF;
END
$$;

WITH canonical_terms AS (
    SELECT DISTINCT
        canonical_name AS phrase,
        canonical_normalized_name AS normalized_phrase,
        canonical_normalized_name AS target_value
    FROM confirmed_manufacturer_aliases

    UNION

    SELECT 'C&S Electric', 'C&S ELECTRIC', 'C&S ELECTRIC'
), inserted_terms AS (
    INSERT INTO catalog_dictionary_terms (
        id,
        phrase,
        normalized_phrase,
        kind,
        target_code,
        target_value,
        priority,
        status,
        source,
        created_at_utc,
        approved_at_utc)
    SELECT
        gen_random_uuid(),
        canonical_term.phrase,
        canonical_term.normalized_phrase,
        'Manufacturer',
        NULL,
        canonical_term.target_value,
        100,
        'Approved',
        'Admin',
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    FROM canonical_terms AS canonical_term
    WHERE NOT EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms AS existing_term
        WHERE existing_term.normalized_phrase = canonical_term.normalized_phrase
          AND existing_term.kind = 'Manufacturer'
          AND existing_term.target_code IS NULL
          AND existing_term.target_value = canonical_term.target_value)
    RETURNING id
)
SELECT 'canonical_dictionary_terms_inserted' AS operation, count(*) AS affected_rows
FROM inserted_terms;

WITH inserted_terms AS (
    INSERT INTO catalog_dictionary_terms (
        id,
        phrase,
        normalized_phrase,
        kind,
        target_code,
        target_value,
        priority,
        status,
        source,
        created_at_utc,
        approved_at_utc)
    SELECT
        gen_random_uuid(),
        confirmed_alias.phrase,
        confirmed_alias.normalized_phrase,
        'Manufacturer',
        NULL,
        confirmed_alias.canonical_normalized_name,
        100,
        'Approved',
        'Admin',
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    FROM confirmed_manufacturer_aliases AS confirmed_alias
    WHERE NOT EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms AS existing_term
        WHERE existing_term.normalized_phrase = confirmed_alias.normalized_phrase
          AND existing_term.kind = 'Manufacturer'
          AND existing_term.target_code IS NULL
          AND existing_term.target_value = confirmed_alias.canonical_normalized_name)
    RETURNING id
)
SELECT 'alias_dictionary_terms_inserted' AS operation, count(*) AS affected_rows
FROM inserted_terms;

WITH canonical_manufacturers AS (
    SELECT DISTINCT
        canonical_name AS name,
        canonical_normalized_name AS normalized_name
    FROM confirmed_manufacturer_aliases

    UNION

    SELECT 'C&S Electric', 'C&S ELECTRIC'
), inserted_manufacturers AS (
    INSERT INTO manufacturers (id, name, normalized_name)
    SELECT
        gen_random_uuid(),
        canonical_manufacturer.name,
        canonical_manufacturer.normalized_name
    FROM canonical_manufacturers AS canonical_manufacturer
    WHERE NOT EXISTS (
        SELECT 1
        FROM manufacturers AS existing_manufacturer
        WHERE existing_manufacturer.normalized_name = canonical_manufacturer.normalized_name)
    ON CONFLICT (normalized_name) DO NOTHING
    RETURNING id
)
SELECT 'canonical_manufacturers_inserted' AS operation, count(*) AS affected_rows
FROM inserted_manufacturers;

CREATE TEMP TABLE confirmed_merge_map
ON COMMIT DROP
AS
SELECT
    source.id AS source_id,
    source.name AS source_name,
    target.id AS target_id,
    target.name AS target_name
FROM confirmed_manufacturer_aliases AS confirmed_alias
JOIN manufacturers AS source
    ON source.normalized_name = confirmed_alias.normalized_phrase
JOIN manufacturers AS target
    ON target.normalized_name = confirmed_alias.canonical_normalized_name
WHERE source.id <> target.id;

SELECT
    source_name AS source_manufacturer,
    target_name AS target_manufacturer,
    count(product.id) AS products_to_move
FROM confirmed_merge_map AS merge_map
LEFT JOIN products AS product
    ON product.manufacturer_id = merge_map.source_id
GROUP BY merge_map.source_id, merge_map.source_name, merge_map.target_name
ORDER BY merge_map.target_name, merge_map.source_name;

WITH moved_products AS (
    UPDATE products AS product
    SET manufacturer_id = merge_map.target_id
    FROM confirmed_merge_map AS merge_map
    WHERE product.manufacturer_id = merge_map.source_id
    RETURNING product.id
)
SELECT 'products_moved_by_confirmed_aliases' AS operation, count(*) AS affected_rows
FROM moved_products;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM products AS product
        JOIN manufacturers AS manufacturer
            ON manufacturer.id = product.manufacturer_id
        JOIN product_types AS product_type
            ON product_type.id = product.product_type_id
        WHERE manufacturer.normalized_name = 'ИНДИЯ'
          AND (
              product_type.code <> 'SWITCH_DISCONNECTOR'
              OR translate(upper(product.name), 'С', 'C') !~ '(CSSD|CSCS)'))
    THEN
        RAISE EXCEPTION
            'Manufacturer "Индия" contains a product that is not a confirmed C&S Electric CSSD/CSCS switch-disconnector. No data was changed.';
    END IF;
END
$$;

WITH moved_products AS (
    UPDATE products AS product
    SET manufacturer_id = target.id
    FROM manufacturers AS source
    CROSS JOIN manufacturers AS target
    WHERE product.manufacturer_id = source.id
      AND source.normalized_name = 'ИНДИЯ'
      AND target.normalized_name = 'C&S ELECTRIC'
    RETURNING product.id
)
SELECT 'india_placeholder_products_moved_to_c_and_s' AS operation, count(*) AS affected_rows
FROM moved_products;

CREATE TEMP TABLE placeholder_product_assignments (
    article text PRIMARY KEY,
    target_manufacturer_normalized_name text NOT NULL
)
ON COMMIT DROP;

INSERT INTO placeholder_product_assignments (
    article,
    target_manufacturer_normalized_name)
VALUES
    ('AUTO-E6BCEC3A3B92', 'HYUNDAI'),
    ('AUTO-2589B1E49C7E', 'DKC'),
    ('AUTO-7CA47C89760B', 'CHINT'),
    ('AUTO-CCEB318D13F9', 'CHINT'),
    ('AUTO-329127096C56', 'CHINT'),
    ('AUTO-C69A0EC41807', 'КЭАЗ');

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM placeholder_product_assignments AS assignment
        LEFT JOIN manufacturers AS target
            ON target.normalized_name = assignment.target_manufacturer_normalized_name
        WHERE target.id IS NULL)
    THEN
        RAISE EXCEPTION
            'A target manufacturer for a "Не указан" product does not exist. No data was changed.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM products AS product
        JOIN manufacturers AS source
            ON source.id = product.manufacturer_id
        LEFT JOIN placeholder_product_assignments AS assignment
            ON upper(btrim(product.article)) = assignment.article
        WHERE source.normalized_name = 'НЕ УКАЗАН'
          AND assignment.article IS NULL)
    THEN
        RAISE EXCEPTION
            'Manufacturer "Не указан" contains an unreviewed product. No data was changed.';
    END IF;
END
$$;

WITH moved_products AS (
    UPDATE products AS product
    SET manufacturer_id = target.id
    FROM manufacturers AS source
    JOIN placeholder_product_assignments AS assignment ON true
    JOIN manufacturers AS target
        ON target.normalized_name = assignment.target_manufacturer_normalized_name
    WHERE product.manufacturer_id = source.id
      AND source.normalized_name = 'НЕ УКАЗАН'
      AND upper(btrim(product.article)) = assignment.article
    RETURNING product.id
)
SELECT 'not_specified_products_reclassified' AS operation, count(*) AS affected_rows
FROM moved_products;

WITH deleted_manufacturers AS (
    DELETE FROM manufacturers AS manufacturer
    WHERE NOT EXISTS (
              SELECT 1
              FROM products AS product
              WHERE product.manufacturer_id = manufacturer.id)
      AND (
          manufacturer.id IN (
              SELECT source_id
              FROM confirmed_merge_map)
          OR manufacturer.normalized_name IN ('ИНДИЯ', 'НЕ УКАЗАН'))
    RETURNING manufacturer.id
)
SELECT 'empty_source_manufacturers_deleted' AS operation, count(*) AS affected_rows
FROM deleted_manufacturers;

DO $$
DECLARE
    products_before bigint;
    products_after bigint;
BEGIN
    SELECT products_count
    INTO products_before
    FROM cleanup_totals;

    SELECT count(*)
    INTO products_after
    FROM products;

    IF products_after <> products_before THEN
        RAISE EXCEPTION
            'Product count changed from % to %. The transaction will be rolled back.',
            products_before,
            products_after;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM products AS product
        JOIN manufacturers AS manufacturer
            ON manufacturer.id = product.manufacturer_id
        WHERE manufacturer.normalized_name IN ('ИНДИЯ', 'НЕ УКАЗАН'))
    THEN
        RAISE EXCEPTION
            'Some placeholder manufacturer products were not reclassified. The transaction will be rolled back.';
    END IF;
END
$$;

COMMIT;

SELECT
    manufacturer.name,
    manufacturer.normalized_name,
    count(product.id) AS products_count
FROM manufacturers AS manufacturer
LEFT JOIN products AS product
    ON product.manufacturer_id = manufacturer.id
GROUP BY manufacturer.id, manufacturer.name, manufacturer.normalized_name
ORDER BY products_count DESC, manufacturer.normalized_name;