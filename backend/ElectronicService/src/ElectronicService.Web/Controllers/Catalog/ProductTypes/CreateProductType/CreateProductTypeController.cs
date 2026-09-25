using ElectronicService.Contracts.Catalog.ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes.CreateProductType;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.CreateProductType;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/product-types")]
public sealed class CreateProductTypeController : ControllerBase
{
    private const string AlreadyExistsCode =
        "catalog.product_type.already_exists";

    private readonly CreateProductTypeCommandHandler _handler;

    public CreateProductTypeController(
        CreateProductTypeCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateProductTypeResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProductTypeResponse>> Create(
        [FromBody] CreateProductTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _handler
            .Handle(
                new CreateProductTypeCommand(
                    request.Code,
                    request.Name),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    AlreadyExistsCode,
                    StringComparison.Ordinal))
            {
                return Conflict(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateProductTypeResponse(
                result.Value.Id,
                result.Value.Code,
                result.Value.Name));
    }
}
