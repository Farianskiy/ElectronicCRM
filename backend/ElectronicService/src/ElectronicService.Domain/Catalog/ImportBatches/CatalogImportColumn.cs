using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ImportBatches;

public sealed class CatalogImportColumn : Abstractions.Entity
{
    public const int MaximumHeaderLength = 500;

    private CatalogImportColumn(
        Guid id,
        Guid batchId,
        int sourceColumnNumber,
        string sourceHeader,
        string normalizedSourceHeader,
        CatalogImportColumnTargetKind targetKind,
        Guid? characteristicDefinitionId,
        decimal confidence,
        bool isConfirmed)
        : base(id)
    {
        BatchId = batchId;
        SourceColumnNumber = sourceColumnNumber;
        SourceHeader = sourceHeader;
        NormalizedSourceHeader =
            normalizedSourceHeader;
        TargetKind = targetKind;
        CharacteristicDefinitionId =
            characteristicDefinitionId;
        Confidence = confidence;
        IsConfirmed = isConfirmed;
    }

    /*
     * Конструктор для EF Core.
     */
    private CatalogImportColumn()
    {
    }

    public Guid BatchId
    {
        get;
        private set;
    }

    /*
     * Номер Excel-колонки, начиная с 1.
     */
    public int SourceColumnNumber
    {
        get;
        private set;
    }

    public string SourceHeader
    {
        get;
        private set;
    } = null!;

    public string NormalizedSourceHeader
    {
        get;
        private set;
    } = null!;

    public CatalogImportColumnTargetKind TargetKind
    {
        get;
        private set;
    }

    /*
     * Заполняется только при:
     *
     * TargetKind == Characteristic.
     */
    public Guid? CharacteristicDefinitionId
    {
        get;
        private set;
    }

    /*
     * Значение от 0 до 1.
     *
     * 1.00 — точное совпадение
     * 0.95 — известный alias
     * 0.85 — нормализованное совпадение
     * 0.00 — назначение не найдено.
     */
    public decimal Confidence
    {
        get;
        private set;
    }

    /*
     * Автоматически найденное соответствие
     * может требовать подтверждения Manager.
     */
    public bool IsConfirmed
    {
        get;
        private set;
    }

    public bool IsMapped =>
        TargetKind
            != CatalogImportColumnTargetKind
                .Unmapped;

    public static Result<
        CatalogImportColumn,
        DomainError> Create(
            Guid batchId,
            int sourceColumnNumber,
            string sourceHeader,
            string normalizedSourceHeader,
            CatalogImportColumnTargetKind targetKind,
            Guid? characteristicDefinitionId,
            decimal confidence,
            bool isConfirmed)
    {
        if (batchId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(batchId));
        }

        if (sourceColumnNumber <= 0)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(sourceColumnNumber));
        }

        if (string.IsNullOrWhiteSpace(
                sourceHeader))
        {
            return GeneralErrors.ValueIsRequired(
                nameof(sourceHeader));
        }

        if (string.IsNullOrWhiteSpace(
                normalizedSourceHeader))
        {
            return GeneralErrors.ValueIsRequired(
                nameof(normalizedSourceHeader));
        }

        var normalizedHeader =
            sourceHeader.Trim();

        var normalizedLookupHeader =
            normalizedSourceHeader.Trim();

        if (normalizedHeader.Length
            > MaximumHeaderLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(sourceHeader),
                MaximumHeaderLength);
        }

        if (normalizedLookupHeader.Length
            > MaximumHeaderLength)
        {
            return GeneralErrors.ValueIsTooLong(
                nameof(normalizedSourceHeader),
                MaximumHeaderLength);
        }

        var mappingValidation =
            ValidateMapping(
                targetKind,
                characteristicDefinitionId,
                confidence,
                isConfirmed);

        if (mappingValidation.IsFailure)
        {
            return mappingValidation.Error;
        }

        return new CatalogImportColumn(
            Guid.CreateVersion7(),
            batchId,
            sourceColumnNumber,
            normalizedHeader,
            normalizedLookupHeader,
            targetKind,
            characteristicDefinitionId,
            confidence,
            isConfirmed);
    }

    /*
     * Позже этот метод будет вызываться
     * редактором сопоставления на frontend.
     */
    public UnitResult<DomainError> ChangeMapping(
        CatalogImportColumnTargetKind targetKind,
        Guid? characteristicDefinitionId,
        bool isConfirmed)
    {
        var validationResult =
            ValidateMapping(
                targetKind,
                characteristicDefinitionId,
                Confidence,
                isConfirmed);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        TargetKind = targetKind;
        CharacteristicDefinitionId =
            characteristicDefinitionId;
        IsConfirmed = isConfirmed;

        return UnitResult.Success<DomainError>();
    }

    private static UnitResult<DomainError>
        ValidateMapping(
            CatalogImportColumnTargetKind targetKind,
            Guid? characteristicDefinitionId,
            decimal confidence,
            bool isConfirmed)
    {
        if (targetKind
            == CatalogImportColumnTargetKind.None)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidColumnMapping());
        }

        if (confidence < 0
            || confidence > 1)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(confidence)));
        }

        if (targetKind
            == CatalogImportColumnTargetKind
                .Characteristic)
        {
            if (characteristicDefinitionId is null
                || characteristicDefinitionId.Value
                    == Guid.Empty)
            {
                return UnitResult.Failure(
                    CatalogImportErrors
                        .InvalidColumnMapping());
            }
        }
        else if (characteristicDefinitionId
                 is not null)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidColumnMapping());
        }

        if (targetKind
                == CatalogImportColumnTargetKind
                    .Unmapped
            && isConfirmed)
        {
            return UnitResult.Failure(
                CatalogImportErrors
                    .InvalidColumnMapping());
        }

        return UnitResult.Success<DomainError>();
    }
}