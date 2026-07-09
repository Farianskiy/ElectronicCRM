namespace ElectronicService.Contracts.Catalog.Dictionaries;

public sealed record CatalogDictionaryTermResponse(
    Guid Id,
    string Phrase,
    string NormalizedPhrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority,
    string Status,
    string Source);