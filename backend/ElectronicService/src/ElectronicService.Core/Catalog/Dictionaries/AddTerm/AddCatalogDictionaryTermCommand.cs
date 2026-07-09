namespace ElectronicService.Core.Catalog.Dictionaries.AddTerm;

public sealed record AddCatalogDictionaryTermCommand(
    string Phrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    int Priority);