namespace ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;

public sealed record GetAvailableCharacteristicDefinitionsQuery(
    string ProductTypeCode,
    string? Search);