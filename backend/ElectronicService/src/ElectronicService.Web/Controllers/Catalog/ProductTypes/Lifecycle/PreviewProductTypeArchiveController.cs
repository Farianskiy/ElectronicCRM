using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.Lifecycle;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.Lifecycle;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types/{productTypeId:guid}/archive/preview")]
public sealed class PreviewProductTypeArchiveController : ControllerBase
{
    private readonly PreviewProductTypeArchiveQueryHandler _handler;

    public PreviewProductTypeArchiveController(
        PreviewProductTypeArchiveQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<ActionResult<ProductTypeArchivePreviewResponse>> Preview(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(
                new PreviewProductTypeArchiveQuery(productTypeId),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(ProductTypeArchiveControllerSupport.Map(result.Value))
            : BadRequest(result.Error.Message);
    }
}
