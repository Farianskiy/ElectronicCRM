using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ImportBatches;

public sealed class CatalogImportRow : Abstractions.Entity
{
    public const int MaximumRawDataJsonLength =
        262_144;

    public const int MaximumNormalizedDataJsonLength =
        262_144;

    public const int MaximumIssuesJsonLength =
        65_536;

    public const int MaximumWarningsJsonLength =
        65_536;

    private CatalogImportRow(
        Guid id,
        Guid batchId,
        int rowNumber,
        CatalogImportRowStatus status,
        string rawDataJson,
        string normalizedDataJson,
        string issuesJson,
        string warningsJson)
        : base(id)
    {
        BatchId = batchId;
        RowNumber = rowNumber;
        Status = status;
        RawDataJson = rawDataJson;
        NormalizedDataJson =
            normalizedDataJson;
        IssuesJson = issuesJson;
        WarningsJson = warningsJson;
    }

    /*
     * Конструктор для EF Core.
     */
    private CatalogImportRow()
    {
    }

    public Guid BatchId
    {
        get;
        private set;
    }

    /*
     * Номер исходной строки Excel.
     *
     * Первая строка является заголовком,
     * поэтому данные начинаются со строки 2.
     */
    public int RowNumber
    {
        get;
        private set;
    }

    public CatalogImportRowStatus Status
    {
        get;
        private set;
    }

    /*
     * Исходные значения, привязанные
     * к номерам Excel-колонок.
     *
     * Пример:
     *
     * 
     *   "1": "Автомат NB1",
     *   "2": "CHINT",
     *   "3": "16"
     * 
     */
    public string RawDataJson
    {
        get;
        private set;
    } = "{}";

    /*
     * Данные после применения mapping.
     *
     * Пример:
     *
     * 
     *   "name": "Автомат NB1",
     *   "article": "NB1-63-1P-C16",
     *   "manufacturer": "CHINT",
     *   "characteristics": 
     *     "...definitionId...": "16"
     *   
     * 
     */
    public string NormalizedDataJson
    {
        get;
        private set;
    } = "{}";

    /*
     * Структурированный JSON-массив ошибок.
     */
    public string IssuesJson
    {
        get;
        private set;
    } = "[]";

    /*
     * Структурированный JSON-массив
     * предупреждений.
     */
    public string WarningsJson
    {
        get;
        private set;
    } = "[]";

    public bool HasErrors =>
        Status == CatalogImportRowStatus.Error;

    public static Result<
        CatalogImportRow,
        DomainError> Create(
            Guid batchId,
            int rowNumber,
            CatalogImportRowStatus status,
            string rawDataJson,
            string normalizedDataJson,
            string issuesJson,
            string warningsJson)
    {
        if (batchId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(batchId));
        }

        if (rowNumber < 2)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(rowNumber));
        }

        if (status
            == CatalogImportRowStatus.None)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(status));
        }

        var rawResult =
            ValidateJson(
                rawDataJson,
                nameof(rawDataJson),
                MaximumRawDataJsonLength);

        if (rawResult.IsFailure)
        {
            return rawResult.Error;
        }

        var normalizedResult =
            ValidateJson(
                normalizedDataJson,
                nameof(normalizedDataJson),
                MaximumNormalizedDataJsonLength);

        if (normalizedResult.IsFailure)
        {
            return normalizedResult.Error;
        }

        var issuesResult =
            ValidateJson(
                issuesJson,
                nameof(issuesJson),
                MaximumIssuesJsonLength);

        if (issuesResult.IsFailure)
        {
            return issuesResult.Error;
        }

        var warningsResult =
            ValidateJson(
                warningsJson,
                nameof(warningsJson),
                MaximumWarningsJsonLength);

        if (warningsResult.IsFailure)
        {
            return warningsResult.Error;
        }

        return new CatalogImportRow(
            Guid.CreateVersion7(),
            batchId,
            rowNumber,
            status,
            rawDataJson,
            normalizedDataJson,
            issuesJson,
            warningsJson);
    }

    /*
     * Позже этот метод будет использоваться
     * при исправлении строки на сайте.
     */
    public UnitResult<DomainError>
        ReplaceValidationResult(
            CatalogImportRowStatus status,
            string normalizedDataJson,
            string issuesJson,
            string warningsJson)
    {
        if (status
            == CatalogImportRowStatus.None)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(status)));
        }

        var normalizedResult =
            ValidateJson(
                normalizedDataJson,
                nameof(normalizedDataJson),
                MaximumNormalizedDataJsonLength);

        if (normalizedResult.IsFailure)
        {
            return normalizedResult;
        }

        var issuesResult =
            ValidateJson(
                issuesJson,
                nameof(issuesJson),
                MaximumIssuesJsonLength);

        if (issuesResult.IsFailure)
        {
            return issuesResult;
        }

        var warningsResult =
            ValidateJson(
                warningsJson,
                nameof(warningsJson),
                MaximumWarningsJsonLength);

        if (warningsResult.IsFailure)
        {
            return warningsResult;
        }

        Status = status;
        NormalizedDataJson =
            normalizedDataJson;
        IssuesJson = issuesJson;
        WarningsJson = warningsJson;

        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError>
        ValidateJson(
            string value,
            string propertyName,
            int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidImportJson(
                        propertyName));
        }

        if (value.Length > maximumLength)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .ImportJsonIsTooLong(
                        propertyName,
                        maximumLength));
        }

        try
        {
            using var document =
                JsonDocument.Parse(value);

            return UnitResult.Success<DomainError>();
        }
        catch (JsonException)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidImportJson(
                        propertyName));
        }
    }
}