namespace ElectronicService.Contracts.Catalog.Products;

public sealed record ProductDetailsResponse(
    Guid Id,
    string Article,
    string Name,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal PriceAmount,
    string PriceCurrency,
    decimal StockQuantity,
    IReadOnlyCollection<ProductCharacteristicResponse> Characteristics,
    IReadOnlyCollection<ProductAliasResponse> Aliases);