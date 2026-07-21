namespace ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;

public sealed record CatalogProductTypeCharacteristicSchemaResult(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int ProductsCount,
    IReadOnlyCollection<
        CatalogProductTypeCharacteristicSchemaItemResult>
        Characteristics);