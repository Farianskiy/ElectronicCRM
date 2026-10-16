using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.ChangeKind;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.ChangeKind;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types/{productTypeId:guid}/kind")]
public sealed class ApplyProductTypeKindChangeController : ControllerBase
{
    private const string BlockedErrorCode =
        "catalog.product_type.kind_change_is_blocked";

    private readonly ChangeProductTypeKindCommandHandler _handler;

    public ApplyProductTypeKindChangeController(
        ChangeProductTypeKindCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPatch]
    [ProducesResponseType(
        typeof(ProductTypeKindChangeResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductTypeKindChangeResponse>> Change(
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
                new ChangeProductTypeKindCommand(
                    productTypeId,
                    targetKind.Value),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return string.Equals(
                    result.Error.Code,
                    BlockedErrorCode,
                    StringComparison.Ordinal)
                ? Conflict(result.Error.Message)
                : BadRequest(result.Error.Message);
        }

        return Ok(ProductTypeKindChangeControllerSupport.Map(result.Value));
    }
}
