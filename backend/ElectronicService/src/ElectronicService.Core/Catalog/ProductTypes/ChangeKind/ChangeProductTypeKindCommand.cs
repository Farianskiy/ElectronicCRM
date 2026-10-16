using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed record ChangeProductTypeKindCommand(
    Guid ProductTypeId,
    ProductTypeKind TargetKind);
