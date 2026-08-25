namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed record AddCatalogDictionaryTermResult(
    Guid Id,
    Guid? ProductTypeId,
    string? ProductTypeCode,
    string Phrase,
    string NormalizedPhrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    string Status,
    string Source,
    int Priority);