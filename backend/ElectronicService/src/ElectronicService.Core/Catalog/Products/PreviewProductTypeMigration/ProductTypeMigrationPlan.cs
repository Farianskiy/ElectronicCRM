using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.Products
    .PreviewProductTypeMigration;

public sealed record ProductTypeMigrationPlan(
    Product Product,
    ProductType CurrentProductType,
    ProductType TargetProductType,
    IReadOnlyDictionary<
        Guid,
        CharacteristicDefinition> DefinitionsById,
    ProductTypeMigrationPreviewResult Preview);