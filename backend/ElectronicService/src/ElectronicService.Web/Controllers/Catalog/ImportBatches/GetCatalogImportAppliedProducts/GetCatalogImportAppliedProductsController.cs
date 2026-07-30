using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportAppliedProductsController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";

    [HttpGet("{batchId:guid}/applied-products")]
    [ProducesResponseType(typeof(GetCatalogImportAppliedProductsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetCatalogImportAppliedProductsResponse>> Get(
        Guid batchId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] GetCatalogImportAppliedProductsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query = new GetCatalogImportAppliedProductsQuery(
            batchId,
            currentUserId,
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 25 : pageSize);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var items = result.Value.Items
            .Select(item => new CatalogImportAppliedProductResponse(
                item.ProductId,
                item.Article,
                item.Name,
                item.ProductTypeCode,
                item.ProductTypeName,
                item.ManufacturerName,
                item.PriceAmount,
                item.PriceCurrency,
                item.StockQuantity,
                item.AppliedAtUtc))
            .ToArray();

        var response = new GetCatalogImportAppliedProductsResponse(
            result.Value.BatchId,
            items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages);

        return Ok(response);
    }
}