namespace ElectronicService.Contracts.Catalog.Dictionaries;

public sealed class SetCatalogDictionaryTermActiveRequest
{
    public bool IsActive { get; init; }

    public string? Reason { get; init; }
}