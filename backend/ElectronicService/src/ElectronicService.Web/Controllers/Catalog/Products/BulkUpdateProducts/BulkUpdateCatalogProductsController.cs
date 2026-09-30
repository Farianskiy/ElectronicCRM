using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.BulkUpdateProducts;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.BulkUpdateProducts;

[ApiController]
[PermissionAnyAuthorize(
    UserPermissionCode.ProductsEdit,
    UserPermissionCode.PricesManage,
    UserPermissionCode.StockManage)]
[Route("api/catalog/products/bulk")]
public sealed class BulkUpdateCatalogProductsController : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(BulkUpdateCatalogProductsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkUpdateCatalogProductsResponse>> Update(
        [FromBody] BulkUpdateCatalogProductsRequest request,
        [FromServices] BulkUpdateCatalogProductsCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var rows = (request.Rows ?? [])
            .Select(row => new BulkUpdateCatalogProductItem(
                row.ProductId,
                row.Name,
                row.Article,
                row.ManufacturerId,
                row.PriceAmount,
                row.PriceCurrency,
                row.StockQuantity))
            .ToArray();

        var command = new BulkUpdateCatalogProductsCommand(
            currentUserId,
            HasPermission(UserPermissionCode.ProductsEdit),
            HasPermission(UserPermissionCode.PricesManage),
            HasPermission(UserPermissionCode.StockManage),
            rows);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return Ok(new BulkUpdateCatalogProductsResponse(
            result.Value.UpdatedProductIds.Count,
            result.Value.UpdatedProductIds));
    }

    private bool HasPermission(UserPermissionCode permission)
    {
        return User.HasClaim(
            PermissionClaimTypes.Permission,
            permission.ToString());
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.current_user.required" => StatusCodes.Status401Unauthorized,
            "catalog.product.bulk.permission_denied" => StatusCodes.Status403Forbidden,
            "catalog.product.not_found" => StatusCodes.Status404NotFound,
            "catalog.manufacturer.not_found" => StatusCodes.Status404NotFound,
            "catalog.product.concurrency_conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось массово обновить товары.",
            Detail = error.Message,
            Type = error.Code
        });
    }
}
