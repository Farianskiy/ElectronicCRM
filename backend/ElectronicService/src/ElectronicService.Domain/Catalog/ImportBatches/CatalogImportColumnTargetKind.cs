namespace ElectronicService.Domain.Catalog.ImportBatches;

public enum CatalogImportColumnTargetKind
{
    None = 0,

    /*
     * Назначение колонки пока не определено.
     */
    Unmapped = 1,

    /*
     * Пользователь решил не импортировать
     * эту колонку.
     */
    Ignore = 2,

    Name = 3,
    Article = 4,
    Manufacturer = 5,
    Price = 6,
    StockQuantity = 7,

    /*
     * Колонка содержит значение одной
     * CharacteristicDefinition.
     */
    Characteristic = 8
}