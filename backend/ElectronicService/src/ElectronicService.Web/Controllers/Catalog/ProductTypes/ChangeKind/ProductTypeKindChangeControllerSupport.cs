using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using ElectronicService.Domain.Catalog.ProductTypes;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.ChangeKind;

internal static class ProductTypeKindChangeControllerSupport
{
    public static ProductTypeKind? ParseKind(string value)
    {
        return Enum.TryParse<ProductTypeKind>(value, true, out var kind)
               && Enum.IsDefined(kind)
            ? kind
            : null;
    }

    public static ProductTypeKindChangeResponse Map(
        ProductTypeKindChangePreview preview)
    {
        return new ProductTypeKindChangeResponse(
            preview.ProductTypeId,
            preview.ProductTypeCode,
            preview.ProductTypeName,
            preview.CurrentKind.ToString(),
            preview.TargetKind.ToString(),
            preview.ProductsCount,
            preview.CharacteristicsCount,
            preview.CanChange,
            preview.Blockers
                .Select(blocker => new ProductTypeKindChangeBlockerResponse(
                    blocker.Code,
                    blocker.Message,
                    blocker.Count))
                .ToArray());
    }
}
