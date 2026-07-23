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

    public static DomainError UserCannotCreateCatalogImport()
    {
        return new DomainError(
            "catalog.import.user.cannot_create",
            "Только активный менеджер или технический " +
            "пользователь может загружать товары.");
    }

    public static DomainError FileCannotBeRead()
    {
        return new DomainError(
            "catalog.import.file.cannot_be_read",
            "Не удалось прочитать загруженный Excel-файл.");
    }
}