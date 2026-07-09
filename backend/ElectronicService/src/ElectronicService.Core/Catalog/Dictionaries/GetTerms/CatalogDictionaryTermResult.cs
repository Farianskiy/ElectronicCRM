namespace ElectronicService.Core.Catalog.Dictionaries.GetTerms;

public sealed record CatalogDictionaryTermResult(
    Guid Id,
    string Phrase,
    string NormalizedPhrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority,
    string Status,
    string Source);