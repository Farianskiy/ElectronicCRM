\set ON_ERROR_STOP on
\pset pager off

SET search_path TO public;
SET client_min_messages TO notice;

BEGIN;

LOCK TABLE manufacturers IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE products IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE catalog_dictionary_terms IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE technical_manufacturer_merge_map
ON COMMIT DROP
AS
WITH manufacturer_statistics AS (
    SELECT
        manufacturer.id,
        manufacturer.name,
        manufacturer.normalized_name,
        btrim(regexp_replace(
            replace(upper(manufacturer.name), 'Ё', 'Е'),
            '[[:space:]]+',
            ' ',
            'g')) AS normalized_key,
        count(product.id) AS products_count
    FROM manufacturers AS manufacturer
    LEFT JOIN products AS product
        ON product.manufacturer_id = manufacturer.id
    GROUP BY manufacturer.id, manufacturer.name, manufacturer.normalized_name
), ranked_manufacturers AS (
    SELECT
        statistics.*,
        row_number() OVER (
            PARTITION BY statistics.normalized_key
            ORDER BY
                (statistics.normalized_name = statistics.normalized_key) DESC,
                statistics.products_count DESC,
                length(statistics.name),
                statistics.id) AS manufacturer_rank
    FROM manufacturer_statistics AS statistics
)
SELECT
    source.id AS source_id,
    target.id AS target_id,
    source.normalized_key
FROM ranked_manufacturers AS source
JOIN ranked_manufacturers AS target
    ON target.normalized_key = source.normalized_key
   AND target.manufacturer_rank = 1
WHERE source.manufacturer_rank > 1;

SELECT
    source.name AS source_manufacturer,
    target.name AS target_manufacturer,
    merge_map.normalized_key
FROM technical_manufacturer_merge_map AS merge_map
JOIN manufacturers AS source ON source.id = merge_map.source_id
JOIN manufacturers AS target ON target.id = merge_map.target_id
ORDER BY merge_map.normalized_key, source.name;

WITH moved_products AS (
    UPDATE products AS product
    SET manufacturer_id = merge_map.target_id
    FROM technical_manufacturer_merge_map AS merge_map
    WHERE product.manufacturer_id = merge_map.source_id
    RETURNING product.id
)
SELECT 'products_moved_by_technical_normalization' AS operation, count(*) AS affected_rows
FROM moved_products;

WITH deleted_manufacturers AS (
    DELETE FROM manufacturers AS manufacturer
    USING technical_manufacturer_merge_map AS merge_map
    WHERE manufacturer.id = merge_map.source_id
      AND NOT EXISTS (
          SELECT 1
          FROM products AS product
          WHERE product.manufacturer_id = manufacturer.id)
    RETURNING manufacturer.id
)
SELECT 'manufacturers_deleted_by_technical_normalization' AS operation, count(*) AS affected_rows
FROM deleted_manufacturers;

UPDATE manufacturers
SET normalized_name = btrim(regexp_replace(
    replace(upper(name), 'Ё', 'Е'),
    '[[:space:]]+',
    ' ',
    'g'))
WHERE normalized_name IS DISTINCT FROM btrim(regexp_replace(
    replace(upper(name), 'Ё', 'Е'),
    '[[:space:]]+',
    ' ',
    'g'));

UPDATE catalog_dictionary_terms
SET normalized_phrase = btrim(regexp_replace(
        replace(upper(phrase), 'Ё', 'Е'),
        '[[:space:]]+',
        ' ',
        'g')),
    target_value = btrim(regexp_replace(
        replace(upper(target_value), 'Ё', 'Е'),
        '[[:space:]]+',
        ' ',
        'g'))
WHERE normalized_phrase IS DISTINCT FROM btrim(regexp_replace(
        replace(upper(phrase), 'Ё', 'Е'),
        '[[:space:]]+',
        ' ',
        'g'))
   OR target_value IS DISTINCT FROM btrim(regexp_replace(
        replace(upper(target_value), 'Ё', 'Е'),
        '[[:space:]]+',
        ' ',
        'g'));

WITH approved_missing_canonical_manufacturers AS (
    SELECT
        alias_term.target_value AS normalized_name,
        min(self_term.phrase) AS display_name
    FROM catalog_dictionary_terms AS alias_term
    JOIN manufacturers AS source
        ON source.normalized_name = alias_term.normalized_phrase
    JOIN catalog_dictionary_terms AS self_term
        ON self_term.kind = 'Manufacturer'
       AND self_term.status = 'Approved'
       AND self_term.normalized_phrase = alias_term.target_value
       AND self_term.target_value = alias_term.target_value
    LEFT JOIN manufacturers AS target
        ON target.normalized_name = alias_term.target_value
    WHERE alias_term.kind = 'Manufacturer'
      AND alias_term.status = 'Approved'
      AND target.id IS NULL
    GROUP BY alias_term.target_value
), inserted_manufacturers AS (
    INSERT INTO manufacturers (id, name, normalized_name)
    SELECT
        gen_random_uuid(),
        display_name,
        normalized_name
    FROM approved_missing_canonical_manufacturers
    ON CONFLICT (normalized_name) DO NOTHING
    RETURNING id
)
SELECT 'approved_canonical_manufacturers_created' AS operation, count(*) AS affected_rows
FROM inserted_manufacturers;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms
        WHERE kind = 'Manufacturer'
          AND status = 'Approved'
        GROUP BY normalized_phrase
        HAVING count(DISTINCT target_value) > 1)
    THEN
        RAISE EXCEPTION
            'Approved manufacturer aliases contain conflicting targets. Run audit-catalog-integrity.sql and resolve the conflicts.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms AS term
        JOIN manufacturers AS source
            ON source.normalized_name = term.normalized_phrase
        LEFT JOIN manufacturers AS target
            ON target.normalized_name = term.target_value
        WHERE term.kind = 'Manufacturer'
          AND term.status = 'Approved'
          AND target.id IS NULL)
    THEN
        RAISE EXCEPTION
            'An approved manufacturer alias points to a missing canonical manufacturer. Run audit-catalog-integrity.sql.';
    END IF;
END
$$;

CREATE TEMP TABLE dictionary_manufacturer_merge_map
ON COMMIT DROP
AS
SELECT DISTINCT
    source.id AS source_id,
    target.id AS target_id,
    term.normalized_phrase,
    term.target_value
FROM catalog_dictionary_terms AS term
JOIN manufacturers AS source
    ON source.normalized_name = term.normalized_phrase
JOIN manufacturers AS target
    ON target.normalized_name = term.target_value
WHERE term.kind = 'Manufacturer'
  AND term.status = 'Approved'
  AND source.id <> target.id;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM dictionary_manufacturer_merge_map AS merge_map
        JOIN dictionary_manufacturer_merge_map AS next_merge
            ON next_merge.source_id = merge_map.target_id)
    THEN
        RAISE EXCEPTION
            'Manufacturer aliases contain a chain. Every alias must point directly to the final canonical manufacturer.';
    END IF;
END
$$;

SELECT
    source.name AS source_manufacturer,
    target.name AS target_manufacturer,
    merge_map.normalized_phrase AS approved_alias
FROM dictionary_manufacturer_merge_map AS merge_map
JOIN manufacturers AS source ON source.id = merge_map.source_id
JOIN manufacturers AS target ON target.id = merge_map.target_id
ORDER BY target.name, source.name;

WITH moved_products AS (
    UPDATE products AS product
    SET manufacturer_id = merge_map.target_id
    FROM dictionary_manufacturer_merge_map AS merge_map
    WHERE product.manufacturer_id = merge_map.source_id
    RETURNING product.id
)
SELECT 'products_moved_by_approved_aliases' AS operation, count(*) AS affected_rows
FROM moved_products;

WITH deleted_manufacturers AS (
    DELETE FROM manufacturers AS manufacturer
    USING dictionary_manufacturer_merge_map AS merge_map
    WHERE manufacturer.id = merge_map.source_id
      AND NOT EXISTS (
          SELECT 1
          FROM products AS product
          WHERE product.manufacturer_id = manufacturer.id)
    RETURNING manufacturer.id
)
SELECT 'manufacturers_deleted_by_approved_aliases' AS operation, count(*) AS affected_rows
FROM deleted_manufacturers;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM catalog_dictionary_terms AS term
        JOIN manufacturers AS source
            ON source.normalized_name = term.normalized_phrase
        JOIN manufacturers AS target
            ON target.normalized_name = term.target_value
        WHERE term.kind = 'Manufacturer'
          AND term.status = 'Approved'
          AND source.id <> target.id)
    THEN
        RAISE EXCEPTION
            'Some approved manufacturer aliases were not merged.';
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