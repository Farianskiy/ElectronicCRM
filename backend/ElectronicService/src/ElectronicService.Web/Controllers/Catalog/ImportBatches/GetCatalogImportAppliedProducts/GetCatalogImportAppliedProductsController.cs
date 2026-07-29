using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportAppliedProductsController : ControllerBase
{
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
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
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
            return ToProblem(result.Error);
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

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.applied_products.unavailable"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось получить товары импорта.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}