namespace ElectronicService.Contracts.Catalog.Dictionaries;

public sealed record AddCatalogDictionaryTermResponse(
    Guid Id,
    Guid? ProductTypeId,
    string? ProductTypeCode,
    string Phrase,
    string NormalizedPhrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority,
    string Status,
    string Source);