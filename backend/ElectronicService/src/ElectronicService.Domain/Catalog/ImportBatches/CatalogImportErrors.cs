using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ImportBatches;

public static class CatalogImportErrors
{
    public static DomainError UnsupportedFileExtension(
        string extension)
    {
        return new DomainError(
            "catalog.import.file.unsupported_extension",
            $"Формат файла '{extension}' не поддерживается. " +
            "Разрешены только файлы .xlsx.");
    }

    public static DomainError FileIsEmpty()
    {
        return new DomainError(
            "catalog.import.file.empty",
            "Загруженный Excel-файл пуст.");
    }

    public static DomainError FileIsTooLarge(
        long maximumSizeBytes)
    {
        return new DomainError(
            "catalog.import.file.too_large",
            $"Размер Excel-файла превышает допустимый предел " +
            $"'{maximumSizeBytes}' байт.");
    }

    public static DomainError InvalidStatusTransition(
        CatalogImportBatchStatus currentStatus,
        CatalogImportBatchStatus targetStatus)
    {
        return new DomainError(
            "catalog.import.batch.invalid_status_transition",
            $"Нельзя перевести пакет импорта из статуса " +
            $"'{currentStatus}' в статус '{targetStatus}'.");
    }

    public static DomainError ProductTypeIsRequired()
    {
        return new DomainError(
            "catalog.import.batch.product_type_required",
            "Перед отправкой или применением импорта " +
            "необходимо выбрать тип товара.");
    }

    public static DomainError InvalidRowsStatistics()
    {
        return new DomainError(
            "catalog.import.batch.invalid_rows_statistics",
            "Статистика строк импорта содержит " +
            "некорректные значения.");
    }

    public static DomainError RejectionReasonIsRequired()
    {
        return new DomainError(
            "catalog.import.batch.rejection_reason_required",
            "При отклонении импорта необходимо указать причину.");
    }

    public static DomainError FailureReasonIsRequired()
    {
        return new DomainError(
            "catalog.import.batch.failure_reason_required",
            "Необходимо указать причину ошибки импорта.");
    }

    public static DomainError CurrentUserNotFound()
    {
        return new DomainError(
            "catalog.import.current_user.not_found",
            "Пользователь, создающий импорт, не найден.");
    }

    public static DomainError BatchRowsCannotBeEdited(
    CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.batch.rows_cannot_be_edited",
            $"Строки пакета импорта в статусе '{status}' нельзя изменять.");
    }

    public static DomainError UserCannotSubmitCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_submit",
            "Пользователь не может отправить этот пакет импорта на проверку.");
    }

    public static DomainError FileCannotBeRead()
    {
        return new DomainError(
            "catalog.import.file.cannot_be_read",
            "Не удалось прочитать загруженный Excel-файл.");
    }

    public static DomainError InvalidColumnMapping()
    {
        return new DomainError(
            "catalog.import.column.invalid_mapping",
            "Сопоставление колонки импорта содержит " +
            "некорректные данные.");
    }

    public static DomainError InvalidImportJson(
        string propertyName)
    {
        return new DomainError(
            "catalog.import.row.invalid_json",
            $"Поле '{propertyName}' должно содержать " +
            "корректный JSON.");
    }

    public static DomainError ImportJsonIsTooLong(
        string propertyName,
        int maximumLength)
    {
        return new DomainError(
            "catalog.import.row.json_too_long",
            $"Размер поля '{propertyName}' превышает " +
            $"допустимый предел '{maximumLength}' символов.");
    }

    public static DomainError BatchNotFound(
    Guid batchId)
    {
        return new DomainError(
            "catalog.import.batch.not_found",
            $"Пакет импорта '{batchId}' не найден.");
    }

    public static DomainError UserCannotAccessBatch()
    {
        return new DomainError(
            "catalog.import.batch.access_denied",
            "Пользователь не может изменять этот пакет импорта.");
    }

    public static DomainError BatchCannotBeAnalyzed(
        CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.batch.cannot_be_analyzed",
            $"Пакет импорта в статусе '{status}' " +
            "нельзя анализировать.");
    }

    public static DomainError ProductTypeNotFound(
        Guid productTypeId)
    {
        return new DomainError(
            "catalog.import.product_type.not_found",
            $"Тип товара '{productTypeId}' не найден.");
    }

    public static DomainError InvalidWorkbook()
    {
        return new DomainError(
            "catalog.import.workbook.invalid",
            "Не удалось прочитать Excel-файл. " +
            "Файл повреждён или не является корректным .xlsx.");
    }

    public static DomainError WorkbookHasNoData()
    {
        return new DomainError(
            "catalog.import.workbook.no_data",
            "Excel-файл не содержит таблицу с данными.");
    }

    public static DomainError WorkbookHasNoHeader()
    {
        return new DomainError(
            "catalog.import.workbook.no_header",
            "Не удалось определить строку заголовков Excel.");
    }

    public static DomainError WorkbookHasTooManyColumns(
        int maximumColumns)
    {
        return new DomainError(
            "catalog.import.workbook.too_many_columns",
            $"Excel-файл содержит больше " +
            $"'{maximumColumns}' используемых колонок.");
    }

    public static DomainError WorkbookHasTooManyRows(
        int maximumRows)
    {
        return new DomainError(
            "catalog.import.workbook.too_many_rows",
            $"Excel-файл содержит больше " +
            $"'{maximumRows}' строк с данными.");
    }

    public static DomainError ColumnNotFound(
    Guid columnId)
    {
        return new DomainError(
            "catalog.import.column.not_found",
            $"Колонка импорта '{columnId}' не найдена.");
    }

    public static DomainError DuplicateColumnMapping()
    {
        return new DomainError(
            "catalog.import.column.duplicate_mapping",
            "Другая колонка уже сопоставлена " +
            "с этим полем каталога.");
    }

    public static DomainError InvalidColumnTarget(
        string targetKind)
    {
        return new DomainError(
            "catalog.import.column.invalid_target",
            $"Назначение колонки '{targetKind}' неизвестно.");
    }

    public static DomainError ProductTypeRequiredForCharacteristic()
    {
        return new DomainError(
            "catalog.import.column.product_type_required",
            "Перед сопоставлением характеристики " +
            "необходимо выбрать тип товара.");
    }

    public static DomainError CharacteristicNotAllowed(
        Guid characteristicDefinitionId,
        Guid productTypeId)
    {
        return new DomainError(
            "catalog.import.column.characteristic_not_allowed",
            $"Характеристика '{characteristicDefinitionId}' " +
            $"не разрешена для типа товара '{productTypeId}'.");
    }

    public static DomainError InvalidPagination()
    {
        return new DomainError(
            "catalog.import.rows.invalid_pagination",
            "Параметры пагинации содержат " +
            "некорректные значения.");
    }

    public static DomainError InvalidRowStatus(
        string status)
    {
        return new DomainError(
            "catalog.import.rows.invalid_status",
            $"Статус строки '{status}' неизвестен.");
    }
}