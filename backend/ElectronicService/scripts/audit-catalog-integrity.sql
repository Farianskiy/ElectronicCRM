\set ON_ERROR_STOP on
\pset pager off

SET search_path TO public;

BEGIN TRANSACTION READ ONLY;

SELECT
    (SELECT count(*) FROM products) AS products_count,
    (SELECT count(*) FROM manufacturers) AS manufacturers_count,
    (SELECT count(*) FROM catalog_dictionary_terms
        WHERE kind = 'Manufacturer' AND status = 'Approved') AS approved_manufacturer_terms_count;

SELECT 'technical_manufacturer_duplicates' AS check_name, count(*) AS problems_count
FROM (
    SELECT btrim(regexp_replace(replace(upper(name), 'Ё', 'Е'), '[[:space:]]+', ' ', 'g'))
    FROM manufacturers
    GROUP BY btrim(regexp_replace(replace(upper(name), 'Ё', 'Е'), '[[:space:]]+', ' ', 'g'))
    HAVING count(*) > 1
) AS problems

UNION ALL

SELECT 'conflicting_approved_manufacturer_aliases', count(*)
FROM (
    SELECT normalized_phrase
    FROM catalog_dictionary_terms
    WHERE kind = 'Manufacturer'
      AND status = 'Approved'
    GROUP BY normalized_phrase
    HAVING count(DISTINCT target_value) > 1
) AS problems

UNION ALL

SELECT 'manufacturer_alias_targets_not_found', count(*)
FROM catalog_dictionary_terms AS term
LEFT JOIN manufacturers AS target
    ON target.normalized_name = term.target_value
WHERE term.kind = 'Manufacturer'
  AND term.status = 'Approved'
  AND target.id IS NULL

UNION ALL

SELECT 'manufacturer_aliases_pending_merge', count(*)
FROM catalog_dictionary_terms AS term
JOIN manufacturers AS source
    ON source.normalized_name = term.normalized_phrase
JOIN manufacturers AS target
    ON target.normalized_name = term.target_value
WHERE term.kind = 'Manufacturer'
  AND term.status = 'Approved'
  AND source.id <> target.id

UNION ALL

SELECT 'products_missing_required_characteristics', count(*)
FROM products AS product
JOIN product_type_characteristics AS type_characteristic
    ON type_characteristic.product_type_id = product.product_type_id
   AND type_characteristic.is_required = true
LEFT JOIN product_characteristic_values AS product_characteristic
    ON product_characteristic.product_id = product.id
   AND product_characteristic.characteristic_definition_id =
       type_characteristic.characteristic_definition_id
WHERE product_characteristic.id IS NULL

UNION ALL

SELECT 'product_characteristics_not_allowed_for_type', count(*)
FROM product_characteristic_values AS product_characteristic
JOIN products AS product
    ON product.id = product_characteristic.product_id
LEFT JOIN product_type_characteristics AS type_characteristic
    ON type_characteristic.product_type_id = product.product_type_id
   AND type_characteristic.characteristic_definition_id =
       product_characteristic.characteristic_definition_id
WHERE type_characteristic.id IS NULL

UNION ALL

SELECT 'duplicate_articles_case_insensitive', count(*)
FROM (
    SELECT upper(btrim(article))
    FROM products
    GROUP BY upper(btrim(article))
    HAVING count(*) > 1
) AS problems

UNION ALL

SELECT 'placeholder_manufacturers', count(*)
FROM manufacturers
WHERE normalized_name IN ('НЕ УКАЗАН', 'UNKNOWN', 'N/A');

SELECT
    btrim(regexp_replace(replace(upper(name), 'Ё', 'Е'), '[[:space:]]+', ' ', 'g')) AS technical_normalized_name,
    array_agg(name ORDER BY name) AS manufacturer_names,
    array_agg(id ORDER BY id) AS manufacturer_ids
FROM manufacturers
GROUP BY btrim(regexp_replace(replace(upper(name), 'Ё', 'Е'), '[[:space:]]+', ' ', 'g'))
HAVING count(*) > 1
ORDER BY technical_normalized_name;

SELECT
    normalized_phrase,
    array_agg(DISTINCT target_value ORDER BY target_value) AS target_values
FROM catalog_dictionary_terms
WHERE kind = 'Manufacturer'
  AND status = 'Approved'
GROUP BY normalized_phrase
HAVING count(DISTINCT target_value) > 1
ORDER BY normalized_phrase;

SELECT
    term.phrase,
    term.normalized_phrase,
    term.target_value
FROM catalog_dictionary_terms AS term
LEFT JOIN manufacturers AS target
    ON target.normalized_name = term.target_value
WHERE term.kind = 'Manufacturer'
  AND term.status = 'Approved'
  AND target.id IS NULL
ORDER BY term.normalized_phrase;

SELECT
    source.id AS source_manufacturer_id,
    source.name AS source_manufacturer,
    target.id AS target_manufacturer_id,
    target.name AS target_manufacturer,
    count(product.id) AS products_to_move
FROM catalog_dictionary_terms AS term
JOIN manufacturers AS source
    ON source.normalized_name = term.normalized_phrase
JOIN manufacturers AS target
    ON target.normalized_name = term.target_value
LEFT JOIN products AS product
    ON product.manufacturer_id = source.id
WHERE term.kind = 'Manufacturer'
  AND term.status = 'Approved'
  AND source.id <> target.id
GROUP BY source.id, source.name, target.id, target.name
ORDER BY products_to_move DESC, source.name;

SELECT
    manufacturer.id,
    manufacturer.name,
    manufacturer.normalized_name,
    count(product.id) AS products_count
FROM manufacturers AS manufacturer
LEFT JOIN products AS product
    ON product.manufacturer_id = manufacturer.id
GROUP BY manufacturer.id, manufacturer.name, manufacturer.normalized_name
ORDER BY products_count, manufacturer.normalized_name;

COMMIT;