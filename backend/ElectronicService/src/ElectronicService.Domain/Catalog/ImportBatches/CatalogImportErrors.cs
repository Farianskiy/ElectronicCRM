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
            "Текущий пользователь не найден.");
    }

    public static DomainError BatchRowsCannotBeEdited(
    CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.batch.rows_cannot_be_edited",
            $"Строки пакета импорта в статусе '{status}' нельзя изменять.");
    }

    public static DomainError UserCannotCreateCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_create",
            "Пользователь не может создать " +
            "пакет импорта. Проверьте его статус " +
            "и права доступа.");
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

    public static DomainError RowNotFound(Guid rowId)
    {
        return new DomainError(
            "catalog.import.row.not_found",
            $"Строка импорта '{rowId}' не найдена.");
    }

    public static DomainError UserCannotEditCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_edit",
            "Пользователь не может редактировать этот пакет импорта.");
    }

    public static DomainError ManufacturerNotFound(Guid manufacturerId)
    {
        return new DomainError(
            "catalog.import.manufacturer.not_found",
            $"Производитель '{manufacturerId}' не найден.");
    }

    public static DomainError UserCannotReviewCatalogImports()
    {
        return new DomainError(
            "catalog.import.user.cannot_review",
            "Пользователь не может проверять пакеты импорта.");
    }

    public static DomainError InvalidReviewQueueStatus(CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.review_queue.invalid_status",
            $"Статус '{status}' нельзя использовать для очереди проверки.");
    }

    public static DomainError BatchConcurrencyConflict()
    {
        return new DomainError(
            "catalog.import.batch.concurrency_conflict",
            "Пакет импорта уже был изменён другим пользователем. Обновите данные и повторите действие.");
    }

    public static DomainError ChangesRequestCommentIsRequired()
    {
        return new DomainError(
            "catalog.import.batch.changes_request_comment_required",
            "При возврате пакета на исправление необходимо указать комментарий.");
    }

    public static DomainError ChangesRequestCommentIsTooLong(int maximumLength)
    {
        return new DomainError(
            "catalog.import.batch.changes_request_comment_too_long",
            $"Комментарий к исправлениям не должен превышать {maximumLength} символов.");
    }

    public static DomainError ReviewIsAssignedToAnotherUser()
    {
        return new DomainError(
            "catalog.import.batch.review_assigned_to_another_user",
            "Проверка этого пакета назначена другому техническому пользователю.");
    }

    public static DomainError RejectionReasonIsTooLong(int maximumLength)
    {
        return new DomainError(
            "catalog.import.batch.rejection_reason_too_long",
            $"Причина отклонения не должна превышать {maximumLength} символов.");
    }

    public static DomainError UserCannotApplyCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_apply",
            "Пользователь не может применять пакеты импорта.");
    }

    public static DomainError BatchCannotBeAppliedByCurrentUser()
    {
        return new DomainError(
            "catalog.import.batch.cannot_be_applied_by_current_user",
            "Текущий пользователь не может применить этот пакет импорта.");
    }

    public static DomainError InvalidNormalizedRow(int rowNumber)
    {
        return new DomainError(
            "catalog.import.apply.invalid_normalized_row",
            $"Нормализованные данные строки Excel '{rowNumber}' некорректны.");
    }

    public static DomainError DuplicateArticleInBatch(
        string article,
        int firstRowNumber,
        int duplicateRowNumber)
    {
        return new DomainError(
            "catalog.import.apply.duplicate_article",
            $"Артикул '{article}' повторяется в строках Excel " +
            $"'{firstRowNumber}' и '{duplicateRowNumber}'.");
    }

    public static DomainError ProductArticleAlreadyExists(
        string article,
        int rowNumber)
    {
        return new DomainError(
            "catalog.import.apply.article_already_exists",
            $"Товар с артикулом '{article}' уже существует. " +
            $"Конфликт обнаружен в строке Excel '{rowNumber}'.");
    }

    public static DomainError CharacteristicDefinitionNotFound(
        Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.import.apply.characteristic_definition_not_found",
            $"Определение характеристики '{characteristicDefinitionId}' не найдено.");
    }

    public static DomainError InvalidCharacteristicValue(
        int rowNumber,
        Guid characteristicDefinitionId)
    {
        return new DomainError(
            "catalog.import.apply.invalid_characteristic_value",
            $"Значение характеристики '{characteristicDefinitionId}' " +
            $"в строке Excel '{rowNumber}' некорректно.");
    }

    public static DomainError ApplyConcurrencyConflict()
    {
        return new DomainError(
            "catalog.import.apply.concurrency_conflict",
            "Во время применения каталог был изменён другим процессом. " +
            "Обновите данные и повторите операцию.");
    }

    public static DomainError ApplyDatabaseFailure()
    {
        return new DomainError(
            "catalog.import.apply.database_failure",
            "Не удалось сохранить результат применения пакета импорта.");
    }

    public static DomainError UserCannotViewOwnCatalogImports()
    {
        return new DomainError(
            "catalog.import.user.cannot_view_own",
            "Пользователь не может просматривать собственные пакеты импорта.");
    }

    public static DomainError InvalidBatchListPagination()
    {
        return new DomainError(
            "catalog.import.batches.invalid_pagination",
            "Параметры пагинации списка импортов содержат некорректные значения.");
    }

    public static DomainError InvalidBatchStatusFilter(CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.batches.invalid_status",
            $"Статус пакета импорта '{status}' нельзя использовать для фильтрации.");
    }

    public static DomainError FileIntegrityCheckFailed()
    {
        return new DomainError(
            "catalog.import.file.integrity_check_failed",
            "Проверка целостности исходного Excel-файла завершилась ошибкой.");
    }

    public static DomainError UserCannotDeleteCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_delete",
            "Пользователь не может удалять пакеты импорта.");
    }

    public static DomainError BatchCannotBeDeleted(CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.batch.cannot_be_deleted",
            $"Пакет импорта в статусе '{status}' нельзя удалить.");
    }

    public static DomainError MappingColumnsAreRequired()
    {
        return new DomainError(
            "catalog.import.mapping.columns_required",
            "Для сохранения сопоставления необходимо передать все колонки Excel.");
    }

    public static DomainError MappingColumnSetMismatch()
    {
        return new DomainError(
            "catalog.import.mapping.column_set_mismatch",
            "Набор переданных колонок не соответствует колонкам текущего анализа.");
    }

    public static DomainError BatchMappingCannotBeEdited(CatalogImportBatchStatus status)
    {
        return new DomainError(
            "catalog.import.mapping.cannot_be_edited",
            $"Сопоставление колонок пакета в статусе '{status}' нельзя изменять.");
    }

}