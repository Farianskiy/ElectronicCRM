namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionMultiIntegerDraftPartItem(
    int Position,
    string? Literal,
    Guid? CharacteristicDefinitionId,
    int DistinctValueCount);

public sealed record CatalogRecognitionMultiIntegerDraftListItem(
    Guid Id,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionMultiIntegerDraftPartItem> Parts,
    int MatchedNameCount,
    int SupportingNameCount,
    int CheckedExampleCount,
    int SupportingExampleCount,
    DateTime CreatedAtUtc);

public sealed record CatalogRecognitionMultiIntegerDraftPage(
    IReadOnlyList<CatalogRecognitionMultiIntegerDraftListItem> Items,
    int Page,
    int PageSize,
    bool HasMore);