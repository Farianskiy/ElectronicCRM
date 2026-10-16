using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.Lifecycle;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.Lifecycle;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types/{productTypeId:guid}/archive")]
public sealed class ArchiveProductTypeController : ControllerBase
{
    private readonly ArchiveProductTypeCommandHandler _handler;

    public ArchiveProductTypeController(ArchiveProductTypeCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<ActionResult<ProductTypeArchivePreviewResponse>> Archive(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(ProductTypeArchiveControllerSupport.Map(result.Value))
            : Conflict(result.Error.Message);
    }
}
