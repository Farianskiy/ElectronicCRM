namespace ElectronicService.Contracts.Catalog.Products.Search;

public sealed record SearchProductCharacteristicRequest(
    string Code,
    string Value);