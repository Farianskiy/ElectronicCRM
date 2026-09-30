using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.CreateProductType;

public sealed record CreateProductTypeCommand(
    string Code,
    string Name,
    ProductTypeKind Kind = ProductTypeKind.MainProduct);
