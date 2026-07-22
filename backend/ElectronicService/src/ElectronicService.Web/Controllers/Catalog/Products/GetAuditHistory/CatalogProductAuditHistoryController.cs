using ElectronicService.Contracts.Catalog.Products
    .AuditHistory;
using ElectronicService.Core.Catalog.Products
    .GetAuditHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .Products.GetAuditHistory;

[ApiController]
[Authorize(Roles = "Technical")]
[Route(
    "api/catalog/products/" +
    "{productId:guid}/audit-history")]
public sealed class
    CatalogProductAuditHistoryController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    [HttpGet]
    [ProducesResponseType(
        typeof(ProductAuditHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<ProductAuditHistoryResponse>> Get(
            [FromRoute] Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromServices]
            GetProductAuditHistoryQueryHandler handler =
                null!,
            CancellationToken cancellationToken =
                default)
    {
        var query =
            new GetProductAuditHistoryQuery(
                productId,
                pageNumber,
                pageSize);

        var result = await handler
            .Handle(
                query,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        var page = result.Value;

        var response =
            new ProductAuditHistoryResponse(
                page.ProductId,
                page.PageNumber,
                page.PageSize,
                page.TotalCount,
                page.TotalPages,
                page.Items
                    .Select(item =>
                        new
                            ProductAuditHistoryItemResponse(
                                item.Id,
                                item.Operation,
                                item.Source,
                                item.SourceId,
                                item.ChangedByUserId,
                                item.ChangedAtUtc,
                                item.Changes
                                    .Select(change =>
                                        new
                                            ProductAuditHistoryChangeResponse(
                                                change.Field,
                                                change.Label,
                                                change.Before,
                                                change.After))
                                    .ToList()))
                    .ToList());

        return Ok(response);
    }
}