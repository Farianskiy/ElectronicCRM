using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceLists;

public static class CatalogPriceListErrors
{
    public static DomainError UnsupportedFileExtension(string extension)
    {
        return new DomainError(
            "catalog.price_list.file.unsupported_extension",
            $"Формат файла '{extension}' не поддерживается. Разрешены только файлы .zip и .xlsx.");
    }

    public static DomainError FileIsEmpty()
    {
        return new DomainError(
            "catalog.price_list.file.empty",
            "Загруженный файл прайс-листа пуст.");
    }

    public static DomainError FileIsTooLarge(long maximumSizeBytes)
    {
        return new DomainError(
            "catalog.price_list.file.too_large",
            $"Размер файла прайс-листа превышает допустимый предел '{maximumSizeBytes}' байт.");
    }

    public static DomainError InvalidStatusTransition(
        CatalogPriceListStatus currentStatus,
        CatalogPriceListStatus targetStatus)
    {
        return new DomainError(
            "catalog.price_list.invalid_status_transition",
            $"Нельзя перевести прайс-лист из статуса '{currentStatus}' в статус '{targetStatus}'.");
    }

    public static DomainError InvalidRowsStatistics()
    {
        return new DomainError(
            "catalog.price_list.invalid_rows_statistics",
            "Статистика строк прайс-листа содержит некорректные значения.");
    }

    public static DomainError FailureReasonIsRequired()
    {
        return new DomainError(
            "catalog.price_list.failure_reason_required",
            "Необходимо указать причину ошибки обработки прайс-листа.");
    }

    public static DomainError PriceCannotBeNegative(
        string propertyName)
    {
        return new DomainError(
            "catalog.price_list.row.price_negative",
            $"Цена '{propertyName}' не может быть отрицательной.");
    }

    public static DomainError InvalidProductUrl()
    {
        return new DomainError(
            "catalog.price_list.row.product_url_invalid",
            "Ссылка на товар должна быть корректным абсолютным HTTP- или HTTPS-адресом.");
    }

    public static DomainError InvalidMatchConfidence()
    {
        return new DomainError(
            "catalog.price_list.row.match_confidence_invalid",
            "Уверенность сопоставления должна находиться в диапазоне от 0 до 100.");
    }

    public static DomainError ProductIdRequiredForMatch()
    {
        return new DomainError(
            "catalog.price_list.row.product_id_required",
            "Для успешного сопоставления необходимо указать товар.");
    }

    public static DomainError InvalidArchive()
    {
        return new DomainError(
            "catalog.price_list.archive.invalid",
            "Не удалось прочитать ZIP-архив прайс-листа.");
    }

    public static DomainError ArchiveMustContainSingleWorkbook(
        int filesCount)
    {
        return new DomainError(
            "catalog.price_list.archive.single_workbook_required",
            $"ZIP-архив должен содержать ровно один Excel-файл. Найдено файлов: {filesCount}.");
    }

    public static DomainError UnsafeArchiveEntry(
        string entryName)
    {
        return new DomainError(
            "catalog.price_list.archive.unsafe_entry",
            $"ZIP-архив содержит небезопасный путь '{entryName}'.");
    }

    public static DomainError ExtractedWorkbookIsTooLarge(
        long maximumSizeBytes)
    {
        return new DomainError(
            "catalog.price_list.archive.extracted_file_too_large",
            $"Размер Excel-файла после распаковки превышает допустимый предел '{maximumSizeBytes}' байт.");
    }

    public static DomainError ArchiveCompressionRatioIsTooHigh()
    {
        return new DomainError(
            "catalog.price_list.archive.compression_ratio_too_high",
            "Степень сжатия ZIP-архива превышает безопасный предел.");
    }

    public static DomainError InvalidWorkbook()
    {
        return new DomainError(
            "catalog.price_list.workbook.invalid",
            "Не удалось прочитать Excel-файл прайс-листа.");
    }

    public static DomainError PriceWorksheetNotFound(
        string worksheetName)
    {
        return new DomainError(
            "catalog.price_list.workbook.worksheet_not_found",
            $"В Excel-файле не найден лист '{worksheetName}'.");
    }

    public static DomainError WorkbookHeaderNotFound()
    {
        return new DomainError(
            "catalog.price_list.workbook.header_not_found",
            "Не удалось найти строку заголовков с колонками 'Артикул' и 'Наименование'.");
    }

    public static DomainError EffectiveDateNotFound()
    {
        return new DomainError(
            "catalog.price_list.workbook.effective_date_not_found",
            "Не удалось определить дату прайс-листа из ячейки B2.");
    }

    public static DomainError WorkbookHasNoRows()
    {
        return new DomainError(
            "catalog.price_list.workbook.no_rows",
            "Excel-файл прайс-листа не содержит товарных строк.");
    }

    public static DomainError WorkbookRowsLimitExceeded(
        int maximumRowsCount)
    {
        return new DomainError(
            "catalog.price_list.workbook.rows_limit_exceeded",
            $"Количество строк прайс-листа превышает допустимый предел '{maximumRowsCount}'.");
    }

    public static DomainError PriceListNotFound(
        Guid priceListId)
    {
        return new DomainError(
            "catalog.price_list.not_found",
            $"Прайс-лист '{priceListId}' не найден.");
    }

    public static DomainError PriceListProcessingFailed()
    {
        return new DomainError(
            "catalog.price_list.processing_failed",
            "Не удалось обработать и сохранить строки прайс-листа.");
    }

    public static DomainError CurrentUserNotFound()
    {
        return new DomainError(
            "catalog.price_list.current_user_not_found",
            "Не удалось определить пользователя, загружающего прайс-лист.");
    }

    public static DomainError UserCannotUploadPriceList()
    {
        return new DomainError(
            "catalog.price_list.upload_forbidden",
            "У пользователя нет прав на загрузку прайс-листов.");
    }

    public static DomainError UserCannotProcessPriceList()
    {
        return new DomainError(
            "catalog.price_list.process_forbidden",
            "У пользователя нет прав на обработку прайс-листов.");
    }

    public static DomainError UserCannotViewPriceList()
    {
        return new DomainError(
            "catalog.price_list.view_forbidden",
            "У пользователя нет прав на просмотр прайс-листов.");
    }

    public static DomainError FileCannotBeRead()
    {
        return new DomainError(
            "catalog.price_list.file.cannot_be_read",
            "Не удалось прочитать поток загружаемого файла.");
    }

    public static DomainError PriceListRowNotFound(
    Guid rowId)
    {
        return new DomainError(
            "catalog.price_list.row.not_found",
            $"Строка прайс-листа '{rowId}' не найдена.");
    }

    public static DomainError ProductDoesNotBelongToManufacturer(
        Guid productId,
        Guid manufacturerId)
    {
        return new DomainError(
            "catalog.price_list.row.product_manufacturer_mismatch",
            $"Товар '{productId}' не принадлежит производителю '{manufacturerId}'.");
    }

    public static DomainError RowsCannotBeEdited(
        CatalogPriceListStatus status)
    {
        return new DomainError(
            "catalog.price_list.rows.cannot_be_edited",
            $"Строки прайс-листа в статусе '{status}' нельзя редактировать.");
    }

    public static DomainError UserCannotEditPriceList()
    {
        return new DomainError(
            "catalog.price_list.edit_forbidden",
            "У пользователя нет прав на редактирование прайс-листа.");
    }

    public static DomainError RowUpdateFailed()
    {
        return new DomainError(
            "catalog.price_list.row.update_failed",
            "Не удалось сохранить изменения строки прайс-листа.");
    }

    public static DomainError BulkRowsRequired()
    {
        return new DomainError(
            "catalog.price_list.bulk.rows_required",
            "Для группового изменения необходимо выбрать хотя бы одну строку.");
    }

    public static DomainError BulkRowsLimitExceeded(
        int maximumRowsCount)
    {
        return new DomainError(
            "catalog.price_list.bulk.rows_limit_exceeded",
            $"За одну операцию можно изменить не более '{maximumRowsCount}' строк.");
    }

    public static DomainError DuplicateBulkRow(
        Guid rowId)
    {
        return new DomainError(
            "catalog.price_list.bulk.duplicate_row",
            $"Строка '{rowId}' передана в групповом запросе несколько раз.");
    }

    public static DomainError UserCannotActivatePriceList()
    {
        return new DomainError(
            "catalog.price_list.activate_forbidden",
            "У пользователя нет прав на активацию прайс-листов.");
    }

    public static DomainError PriceListActivationFailed()
    {
        return new DomainError(
            "catalog.price_list.activation_failed",
            "Не удалось активировать прайс-лист. Возможно, состояние версии было изменено другим пользователем.");
    }

    public static DomainError InvalidIssueGroupKey()
    {
        return new DomainError(
            "catalog.price_list.issue_group.invalid_key",
            "Ключ группы ошибок имеет некорректный формат.");
    }

    public static DomainError IssueGroupNotFound(
        string groupKey)
    {
        return new DomainError(
            "catalog.price_list.issue_group.not_found",
            $"Группа ошибок '{groupKey}' не найдена или уже была исправлена.");
    }

    public static DomainError IssueGroupCorrectionIsAmbiguous()
    {
        return new DomainError(
            "catalog.price_list.issue_group.correction_ambiguous",
            "Нельзя одновременно изменять единицу измерения и назначать товар.");
    }

    public static DomainError IssueGroupFieldMismatch(
        string field)
    {
        return new DomainError(
            "catalog.price_list.issue_group.field_mismatch",
            $"Групповое исправление не поддерживается для поля '{field}'.");
    }

    public static DomainError IssueGroupUnitTooLong(
        int maximumLength)
    {
        return new DomainError(
            "catalog.price_list.issue_group.unit_too_long",
            $"Единица измерения содержит больше '{maximumLength}' символов.");
    }
}