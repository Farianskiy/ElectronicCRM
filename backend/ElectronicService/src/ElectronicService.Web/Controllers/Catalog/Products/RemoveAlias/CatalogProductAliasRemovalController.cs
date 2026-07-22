using ElectronicService.Core.Catalog.Products.RemoveAlias;
using Microsoft.AspNetCore.Authorization;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.RemoveAlias;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/{id:guid}/aliases/{aliasId:guid}")]
public sealed class CatalogProductAliasRemovalController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    private const string ProductAliasNotFoundCode =
        "catalog.product_alias.not_found";

    private const string ProductConcurrencyConflictCode =
        "catalog.product.concurrency_conflict";

    private readonly RemoveProductAliasCommandHandler _handler;

    public CatalogProductAliasRemovalController(
        RemoveProductAliasCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveAlias(
        Guid id,
        Guid aliasId,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(
        out var changedByUserId))
        {
            return Unauthorized();
        }

        var command = new RemoveProductAliasCommand(
            id,
            changedByUserId,
            aliasId);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ProductAliasNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(result.Error.Message);
            }

            if (string.Equals(
                result.Error.Code,
                ProductConcurrencyConflictCode,
                StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}