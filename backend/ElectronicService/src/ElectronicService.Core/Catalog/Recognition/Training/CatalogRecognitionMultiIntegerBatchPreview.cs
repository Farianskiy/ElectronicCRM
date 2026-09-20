namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerBatchPreviewRequest(
    Guid BatchId,
    Guid ManufacturerId,
    Guid ProductTypeId,
    IReadOnlyList<Guid> CharacteristicDefinitionIds,
    IReadOnlyList<string> ProductNames,
    string GeneratorVersion,
    CatalogRecognitionMultiIntegerPattern Pattern,
    int Page,
    int PageSize);

public sealed record CatalogRecognitionMultiIntegerBatchPreviewItem(
    CatalogRecognitionMultiIntegerRowPreviewResult Result,
    bool MatchesSelectedTrainingName);

public sealed record CatalogRecognitionMultiIntegerBatchPreviewPage(
    Guid BatchId,
    string GeneratorVersion,
    CatalogRecognitionMultiIntegerPattern Pattern,
    DateTime CheckedAtUtc,
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<CatalogRecognitionMultiIntegerBatchPreviewItem> Items);