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
    Guid? MainProductTypeId,
    Guid? NeedDefinitionId,
    bool IsActive,
    string Status,
    string? Message,
    IReadOnlyList<ComponentCompatibilityImportConstraintResponse> Constraints);

public sealed record ComponentCompatibilityImportConstraintResponse(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value,
    string ComparisonOperator);

public sealed record ApplyComponentCompatibilityImportRequest(
    IReadOnlyList<ApplyComponentCompatibilityImportRowRequest>? Rows);

public sealed record ApplyComponentCompatibilityImportRowRequest(
    Guid ComponentProductId,
    Guid MainProductTypeId,
    Guid? NeedDefinitionId,
    bool IsActive,
    IReadOnlyList<ComponentCompatibilityImportConstraintRequest>? Constraints);

public sealed record ComponentCompatibilityImportConstraintRequest(
    Guid CharacteristicDefinitionId,
    string CharacteristicName,
    string Value,
    string ComparisonOperator = "Equal");

public sealed record ApplyComponentCompatibilityImportResponse(
    int CreatedRulesCount);
