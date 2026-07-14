\set ON_ERROR_STOP on
\pset pager off

SET search_path TO public;

BEGIN TRANSACTION READ ONLY;

SELECT
    manufacturer.name AS manufacturer,
    product_type.code AS product_type,
    count(product.id) AS products_count
FROM manufacturers AS manufacturer
LEFT JOIN products AS product
    ON product.manufacturer_id = manufacturer.id
LEFT JOIN product_types AS product_type
    ON product_type.id = product.product_type_id
WHERE manufacturer.normalized_name IN (
    'IEK ПРОМ СЕРИЯ',
    'DEKRAFT ПРОМ СЕРИЯ',
    'АВВ ПРОМ СЕРИЯ',
    'LEGRAND ПРОМ СЕРИЯ',
    'LSIS ПРОМ СЕРИЯ',
    'KEAZ',
    'КЭАЗ',
    'TDM',
    'ТДМ',
    'DKC',
    'ДКС',
    'ШНАЙДЕР ПРОМ',
    'ШНАЙДЕР CVS',
    'ШНАЙДЕР EZC',
    'SYSTEME EL',
    'ИНДИЯ',
    'НЕ УКАЗАН'
)
GROUP BY manufacturer.name, product_type.code
ORDER BY manufacturer.name, products_count DESC;

WITH ranked_products AS (
    SELECT
        manufacturer.name AS manufacturer,
        product.article,
        product.name AS product_name,
        product_type.code AS product_type,
        row_number() OVER (
            PARTITION BY manufacturer.id
            ORDER BY product.name
        ) AS row_number
    FROM manufacturers AS manufacturer
    JOIN products AS product
        ON product.manufacturer_id = manufacturer.id
    JOIN product_types AS product_type
        ON product_type.id = product.product_type_id
    WHERE manufacturer.normalized_name IN (
        'IEK ПРОМ СЕРИЯ',
        'DEKRAFT ПРОМ СЕРИЯ',
        'АВВ ПРОМ СЕРИЯ',
        'KEAZ',
        'КЭАЗ',
        'TDM',
        'ТДМ',
        'DKC',
        'ДКС',
        'ШНАЙДЕР ПРОМ',
        'ШНАЙДЕР CVS',
        'SYSTEME EL',
        'ИНДИЯ',
        'НЕ УКАЗАН'
    )
)
SELECT
    manufacturer,
    article,
    product_name,
    product_type
FROM ranked_products
WHERE row_number <= 10
ORDER BY manufacturer, row_number;

COMMIT;