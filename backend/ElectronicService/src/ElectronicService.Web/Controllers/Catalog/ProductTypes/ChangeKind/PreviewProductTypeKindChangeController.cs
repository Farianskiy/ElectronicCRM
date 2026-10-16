using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.ChangeKind;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types/{productTypeId:guid}/kind/preview")]
public sealed class PreviewProductTypeKindChangeController : ControllerBase
{
    private readonly PreviewProductTypeKindChangeQueryHandler _handler;

    public PreviewProductTypeKindChangeController(
        PreviewProductTypeKindChangeQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ProductTypeKindChangeResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductTypeKindChangeResponse>> Preview(
        Guid productTypeId,
        [FromBody] ChangeProductTypeKindRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetKind = ProductTypeKindChangeControllerSupport
            .ParseKind(request.TargetKind);

        if (targetKind is null)
        {
            return BadRequest(
                "Назначение типа товара должно быть MainProduct или Component.");
        }

        var result = await _handler
            .Handle(
                new PreviewProductTypeKindChangeQuery(
                    productTypeId,
                    targetKind.Value),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(ProductTypeKindChangeControllerSupport.Map(result.Value))
            : BadRequest(result.Error.Message);
    }
}
