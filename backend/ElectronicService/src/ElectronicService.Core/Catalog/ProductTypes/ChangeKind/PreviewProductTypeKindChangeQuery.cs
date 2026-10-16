using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.ChangeKind;

public sealed record PreviewProductTypeKindChangeQuery(
    Guid ProductTypeId,
    ProductTypeKind TargetKind);
