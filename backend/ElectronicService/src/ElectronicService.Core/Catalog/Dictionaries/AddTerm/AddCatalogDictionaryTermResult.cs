namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed record AddCatalogDictionaryTermResult(
    Guid Id,
    string Phrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    string Status,
    string Source,
    int Priority);