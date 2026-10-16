using CSharpFunctionalExtensions;
using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.ArchiveProduct;
using ElectronicService.Core.Catalog.Products.RestoreProduct;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/{id:guid}")]
[PermissionAuthorize(UserPermissionCode.ProductsEdit)]
public sealed class CatalogProductArchiveController : ControllerBase
{
    private const string ProductNotFoundCode = "catalog.product.not_found";
    private const string ProductConcurrencyConflictCode =
        "catalog.product.concurrency_conflict";

    [HttpPost("archive")]
    public async Task<IActionResult> Archive(
        Guid id,
        ArchiveProductRequest request,
        [FromServices] ArchiveProductCommandHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await handler.Handle(
            new ArchiveProductCommand(id, userId, request.Reason),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] RestoreProductCommandHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await handler.Handle(
            new RestoreProductCommand(id, userId),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult(UnitResult<DomainError> result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (string.Equals(result.Error.Code, ProductNotFoundCode, StringComparison.Ordinal))
        {
            return NotFound(result.Error.Message);
        }

        if (string.Equals(
            result.Error.Code,
            ProductConcurrencyConflictCode,
            StringComparison.Ordinal))
        {
            return Conflict(result.Error.Message);
        }

        return BadRequest(result.Error.Message);
    }
}
