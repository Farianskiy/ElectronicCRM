using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Components;

public enum ComponentCompatibilityImportRowStatus
{
    Ready = 0,
    AlreadyExists = 1,
    Invalid = 2,
    ComponentNotFound = 3,
    MainProductTypeNotFound = 4,
    NeedNotFound = 5,
    CharacteristicNotFound = 6,
    Duplicate = 7,
    WillCreate = 8,
    WillUpdate = 9,
    Unchanged = 10
}

public sealed record ComponentCompatibilityImportConstraint(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value,
    ComponentCompatibilityComparisonOperator ComparisonOperator =
        ComponentCompatibilityComparisonOperator.Equal);

public sealed record ComponentCompatibilityImportPreviewRow(
    int RowNumber,
    string ComponentArticle,
    string MainProductType,
    string Need,
    Guid? ComponentProductId,
    Guid? NeedDefinitionId,
    bool IsActive,
    ComponentCompatibilityImportRowStatus Status,
    string? Message,
    IReadOnlyList<ComponentCompatibilityImportConstraint> Constraints);

public sealed record ComponentCompatibilityImportPreview(
    int ReadRowsCount,
    int ReadyRowsCount,
    int SkippedRowsCount,
    IReadOnlyList<ComponentCompatibilityImportPreviewRow> Rows);

public sealed record ApplyComponentCompatibilityImportRow(
    Guid ComponentProductId,
    Guid NeedDefinitionId,
    bool IsActive,
    IReadOnlyList<ComponentCompatibilityImportConstraint> Constraints);

public interface ICatalogComponentCompatibilityWorkbookService
{
    Task<Result<byte[], DomainError>> CreateTemplateAsync(
        string? mainProductTypeCode,
        IReadOnlyCollection<string>? characteristicCodes = null,
        IReadOnlyCollection<string>? rangeCharacteristicCodes = null,
        CancellationToken cancellationToken = default);

    Task<Result<byte[], DomainError>> ExportRulesAsync(
        string mainProductTypeCode,
        CancellationToken cancellationToken = default);

    Task<Result<ComponentCompatibilityImportPreview, DomainError>> PreviewAsync(
        Stream workbookStream,
        CancellationToken cancellationToken = default);

    Task<Result<int, DomainError>> ApplyAsync(
        IReadOnlyList<ApplyComponentCompatibilityImportRow> rows,
        CancellationToken cancellationToken = default);
}
