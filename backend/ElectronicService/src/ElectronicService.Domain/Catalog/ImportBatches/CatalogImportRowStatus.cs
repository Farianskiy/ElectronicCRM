namespace ElectronicService.Domain.Catalog.ImportBatches;

public enum CatalogImportRowStatus
{
    None = 0,

    /*
     * Полная проверка невозможна,
     * пока не закончено сопоставление колонок.
     */
    PendingMapping = 1,

    /*
     * Строка прошла текущую валидацию.
     */
    Valid = 2,

    /*
     * В строке есть ошибки, которые
     * необходимо исправить на сайте.
     */
    Error = 3
}