namespace ElectronicService.Contracts.Catalog.ProductTypes;

public sealed record
    CatalogProductTypeCharacteristicSchemaResponse(
        Guid ProductTypeId,
        string ProductTypeCode,
        string ProductTypeName,
        int ProductsCount,
        IReadOnlyCollection<
            CatalogProductTypeCharacteristicSchemaItemResponse>
            Characteristics);