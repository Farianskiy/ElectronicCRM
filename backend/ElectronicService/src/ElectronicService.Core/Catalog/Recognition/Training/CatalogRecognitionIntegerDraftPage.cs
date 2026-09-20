namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record CatalogRecognitionIntegerDraftListItem(
    Guid Id,
    string Prefix,
    IReadOnlyList<string> Suffixes,
    string GeneratorVersion,
    int MatchedNameCount,
    int DistinctValueCount,
    int CheckedExampleCount,
    int SupportingExampleCount,
    DateTime CreatedAtUtc);

public sealed record CatalogRecognitionIntegerDraftPage(
    IReadOnlyList<CatalogRecognitionIntegerDraftListItem> Items,
    int Page,
    int PageSize,
    bool HasMore);