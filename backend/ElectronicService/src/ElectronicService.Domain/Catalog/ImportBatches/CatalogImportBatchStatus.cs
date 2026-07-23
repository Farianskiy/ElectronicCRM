namespace ElectronicService.Domain.Catalog.ImportBatches;

public enum CatalogImportBatchStatus
{
    None = 0,

    /*
     * Файл сохранён, но ещё не разобран.
     */
    Uploaded = 1,

    /*
     * Некоторые Excel-колонки не удалось
     * сопоставить с полями каталога.
     */
    MappingRequired = 2,

    /*
     * Колонки определены, но в строках
     * остались ошибки или пустые значения.
     */
    NeedsCorrection = 3,

    /*
     * Все обязательные данные заполнены.
     */
    Ready = 4,

    /*
     * Manager отправил batch
     * техническому пользователю.
     */
    Submitted = 5,

    /*
     * Technical начал проверку.
     */
    UnderReview = 6,

    /*
     * Данные записываются в основной каталог.
     */
    Applying = 7,

    /*
     * Данные успешно применены.
     */
    Applied = 8,

    /*
     * Technical отклонил batch.
     */
    Rejected = 9,

    /*
     * Во время обработки произошла ошибка.
     */
    Failed = 10
}