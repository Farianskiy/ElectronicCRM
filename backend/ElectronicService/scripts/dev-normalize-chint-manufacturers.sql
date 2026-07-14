-- Dev normalization for CHINT manufacturers.
-- Local development only.
-- Do not run this in production without backup/review.

SET client_encoding TO 'UTF8';

BEGIN;

SELECT
    'before' AS stage,
    m.id,
    m.name,
    m.normalized_name,
    count(p.id) AS products_count
FROM manufacturers m
LEFT JOIN products p ON p.manufacturer_id = m.id
WHERE m.normalized_name IN ('CHINT', 'ЧИНТ', 'CHINT ПРОМ СЕРИЯ')
GROUP BY m.id, m.name, m.normalized_name
ORDER BY products_count DESC;

DO $$
DECLARE
    canonical_chint_id uuid;
    moved_products_count integer;
    deleted_manufacturers_count integer;
BEGIN
    SELECT id
    INTO canonical_chint_id
    FROM manufacturers
    WHERE normalized_name = 'CHINT'
    ORDER BY name
    LIMIT 1;

    IF canonical_chint_id IS NULL THEN
        RAISE EXCEPTION 'Canonical manufacturer CHINT was not found.';
    END IF;

    UPDATE products
    SET manufacturer_id = canonical_chint_id,
        updated_at_utc = now()
    WHERE manufacturer_id IN (
        SELECT id
        FROM manufacturers
        WHERE normalized_name IN ('ЧИНТ', 'CHINT ПРОМ СЕРИЯ')
    );

    GET DIAGNOSTICS moved_products_count = ROW_COUNT;

    DELETE FROM manufacturers
    WHERE normalized_name IN ('ЧИНТ', 'CHINT ПРОМ СЕРИЯ');

    GET DIAGNOSTICS deleted_manufacturers_count = ROW_COUNT;

    RAISE NOTICE 'Moved products to CHINT: %', moved_products_count;
    RAISE NOTICE 'Deleted duplicate manufacturers: %', deleted_manufacturers_count;
END $$;

DELETE FROM catalog_assistant_dictionary_suggestions
WHERE normalized_unknown_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ')
   OR suggested_target_value IN ('ЧИНТ', 'CHINT ПРОМ СЕРИЯ');

DELETE FROM catalog_dictionary_terms
WHERE normalized_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ')
   OR target_value IN ('ЧИНТ', 'CHINT ПРОМ СЕРИЯ');

SELECT
    'after' AS stage,
    m.id,
    m.name,
    m.normalized_name,
    count(p.id) AS products_count
FROM manufacturers m
LEFT JOIN products p ON p.manufacturer_id = m.id
WHERE m.normalized_name IN ('CHINT', 'ЧИНТ', 'CHINT ПРОМ СЕРИЯ')
GROUP BY m.id, m.name, m.normalized_name
ORDER BY products_count DESC;

COMMIT;