namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionLiteralDraftListItem(
    Guid Id,
    Guid ManufacturerId,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string Literal,
    string NormalizedValue,
    string GeneratorVersion,
    int MatchedNameCount,
    int CheckedExampleCount,
    int SupportingExampleCount,
    DateTime CreatedAtUtc);

public sealed record CatalogRecognitionLiteralDraftPage(
    IReadOnlyList<CatalogRecognitionLiteralDraftListItem> Items,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record CatalogRecognitionLiteralDraftEvidenceDetails(
    Guid ExampleId,
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    bool IsSupporting,
    DateTime ConfirmedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record CatalogRecognitionLiteralDraftDetails(
    CatalogRecognitionLiteralDraftListItem Draft,
    IReadOnlyList<CatalogRecognitionLiteralDraftEvidenceDetails> Evidence);