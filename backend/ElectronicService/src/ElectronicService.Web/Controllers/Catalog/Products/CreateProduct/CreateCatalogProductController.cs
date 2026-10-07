using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.CreateProduct;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.CreateProduct;

[PermissionAuthorize(UserPermissionCode.ProductsEdit)]
[ApiController]
[Route("api/catalog/products")]
public sealed class CreateCatalogProductController : ControllerBase
{
    private readonly CreateProductCommandHandler _handler;

    public CreateCatalogProductController(CreateProductCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateCatalogProductResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCatalogProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _handler
            .Handle(
                new CreateProductCommand(
                    currentUserId,
                    request.Article,
                    request.Name,
                    request.ProductTypeId,
                    request.ManufacturerId,
                    request.PriceAmount,
                    request.StockQuantity,
                    (request.Characteristics ?? [])
                        .Select(item => new CreateProductCharacteristic(
                            item.Code,
                            item.Value))
                        .ToArray()),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    "catalog.product.already_exists",
                    StringComparison.Ordinal))
            {
                return Conflict(result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    "catalog.product_type.not_found",
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    "catalog.manufacturer.not_found",
                    StringComparison.Ordinal))
            {
                return NotFound(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        var response = new CreateCatalogProductResponse(
            result.Value.ProductId,
            result.Value.Article,
            result.Value.Name,
            result.Value.ProductTypeId,
            result.Value.ManufacturerId,
            result.Value.PriceAmount,
            result.Value.StockQuantity);

        return CreatedAtAction(
            nameof(Create),
            new { id = response.ProductId },
            response);
    }
}
