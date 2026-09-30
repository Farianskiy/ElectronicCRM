using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.CreateProductType;

public sealed record CreateProductTypeResult(
    Guid Id,
    string Code,
    string Name,
    ProductTypeKind Kind);
