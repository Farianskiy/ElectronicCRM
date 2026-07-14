-- Dev reset for assistant dictionary flow.
-- Local development only.
-- Removes repeated test aliases for the assistant dictionary flow.

SET client_encoding TO 'UTF8';

BEGIN;

SELECT
    'suggestions_before' AS check_name,
    count(*) AS rows_count
FROM catalog_assistant_dictionary_suggestions
WHERE normalized_unknown_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

SELECT
    'dictionary_terms_before' AS check_name,
    count(*) AS rows_count
FROM catalog_dictionary_terms
WHERE normalized_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

DELETE FROM catalog_assistant_dictionary_suggestions
WHERE normalized_unknown_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

DELETE FROM catalog_dictionary_terms
WHERE normalized_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

SELECT
    'suggestions_after' AS check_name,
    count(*) AS rows_count
FROM catalog_assistant_dictionary_suggestions
WHERE normalized_unknown_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

SELECT
    'dictionary_terms_after' AS check_name,
    count(*) AS rows_count
FROM catalog_dictionary_terms
WHERE normalized_phrase IN ('ЧЕНТ', 'ЧИНТ', 'ЧНТ', 'ЧАНТ');

COMMIT;