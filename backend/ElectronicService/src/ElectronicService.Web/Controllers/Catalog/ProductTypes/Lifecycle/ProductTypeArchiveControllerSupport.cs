using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.Lifecycle;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.Lifecycle;

internal static class ProductTypeArchiveControllerSupport
{
    public static ProductTypeArchivePreviewResponse Map(
        ProductTypeArchivePreview preview)
    {
        return new ProductTypeArchivePreviewResponse(
            preview.ProductTypeId,
            preview.ProductTypeCode,
            preview.ProductTypeName,
            preview.IsArchived,
            preview.ProductsCount,
            preview.CharacteristicsCount,
            preview.CanArchive,
            preview.Blockers.Select(blocker =>
                new ProductTypeArchiveBlockerResponse(
                    blocker.Code,
                    blocker.Message,
                    blocker.Count)).ToArray());
    }
}
