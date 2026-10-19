namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

using ElectronicService.Domain.Catalog.ProductTypes;

internal sealed record CatalogProductTypeSeed(
    string Code,
    string Name,
    ProductTypeKind Kind = ProductTypeKind.MainProduct);
