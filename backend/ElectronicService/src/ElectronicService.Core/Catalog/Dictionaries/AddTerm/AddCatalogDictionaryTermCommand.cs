namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed record AddCatalogDictionaryTermCommand(
    string? ProductTypeCode,
    string Phrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority);