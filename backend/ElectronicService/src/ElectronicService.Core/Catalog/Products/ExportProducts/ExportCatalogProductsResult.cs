namespace ElectronicService.Core.Catalog.Products.ExportProducts;

public sealed record ExportCatalogProductsResult(
    ReadOnlyMemory<byte> Content,
    string ContentType,
    string FileName,
    int ProductsCount);
