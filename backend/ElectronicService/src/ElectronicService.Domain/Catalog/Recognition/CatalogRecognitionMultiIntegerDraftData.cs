namespace ElectronicService.Domain.Catalog.Recognition;

public sealed record CatalogRecognitionMultiIntegerDraftPartData(
    string? Literal,
    Guid? CharacteristicDefinitionId,
    int DistinctValueCount);

public sealed record CatalogRecognitionMultiIntegerDraftData(
    Guid ManufacturerId,
    Guid ProductTypeId,
    string GeneratorVersion,
    IReadOnlyList<CatalogRecognitionMultiIntegerDraftPartData> Parts,
    int MatchedNameCount,
    int SupportingNameCount,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> CheckedExampleIds,
    IReadOnlyCollection<Guid> SupportingExampleIds);