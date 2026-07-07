namespace ElectronicService.Contracts.Catalog.Products;

public sealed record ProductDetailsResponse(
    Guid Id,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    IReadOnlyCollection<ProductCharacteristicResponse> Characteristics,
    IReadOnlyCollection<string> Aliases);