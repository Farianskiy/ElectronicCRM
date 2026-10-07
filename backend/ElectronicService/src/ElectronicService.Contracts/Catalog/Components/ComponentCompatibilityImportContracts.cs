namespace ElectronicService.Contracts.Catalog.Components;

public sealed record PreviewComponentCompatibilityImportResponse(
    int ReadRowsCount,
    int ReadyRowsCount,
    int SkippedRowsCount,
    IReadOnlyList<ComponentCompatibilityImportPreviewRowResponse> Rows);

public sealed record ComponentCompatibilityImportPreviewRowResponse(
    int RowNumber,
    string ComponentArticle,
    string MainProductType,
    string Need,
    Guid? ComponentProductId,
    Guid? NeedDefinitionId,
    string Status,
    string? Message,
    IReadOnlyList<ComponentCompatibilityImportConstraintResponse> Constraints);

public sealed record ComponentCompatibilityImportConstraintResponse(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value);

public sealed record ApplyComponentCompatibilityImportRequest(
    IReadOnlyList<ApplyComponentCompatibilityImportRowRequest>? Rows);

public sealed record ApplyComponentCompatibilityImportRowRequest(
    Guid ComponentProductId,
    Guid NeedDefinitionId,
    IReadOnlyList<ComponentCompatibilityImportConstraintRequest>? Constraints);

public sealed record ComponentCompatibilityImportConstraintRequest(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value);

public sealed record ApplyComponentCompatibilityImportResponse(
    int CreatedRulesCount);
