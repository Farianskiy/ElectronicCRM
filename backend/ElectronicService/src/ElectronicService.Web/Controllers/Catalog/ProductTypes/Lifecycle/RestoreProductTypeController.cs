using ElectronicService.Core.Catalog.ProductTypes.Lifecycle;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.Lifecycle;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types/{productTypeId:guid}/restore")]
public sealed class RestoreProductTypeController : ControllerBase
{
    private readonly RestoreProductTypeCommandHandler _handler;

    public RestoreProductTypeController(RestoreProductTypeCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Restore(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error.Message);
    }
}
