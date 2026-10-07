using CSharpFunctionalExtensions;
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
    Duplicate = 7
}

public sealed record ComponentCompatibilityImportConstraint(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value);

public sealed record ComponentCompatibilityImportPreviewRow(
    int RowNumber,
    string ComponentArticle,
    string MainProductType,
    string Need,
    Guid? ComponentProductId,
    Guid? NeedDefinitionId,
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
    IReadOnlyList<ComponentCompatibilityImportConstraint> Constraints);

public interface ICatalogComponentCompatibilityWorkbookService
{
    byte[] CreateTemplate();

    Task<Result<ComponentCompatibilityImportPreview, DomainError>> PreviewAsync(
        Stream workbookStream,
        CancellationToken cancellationToken = default);

    Task<Result<int, DomainError>> ApplyAsync(
        IReadOnlyList<ApplyComponentCompatibilityImportRow> rows,
        CancellationToken cancellationToken = default);
}
