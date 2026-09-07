using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListRows;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.GetCatalogPriceListRows;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class GetCatalogPriceListRowsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить строки прайс-листа.";

    [HttpGet("{priceListId:guid}/rows")]
    [ProducesResponseType(
        typeof(GetCatalogPriceListRowsResponse),
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
        ActionResult<GetCatalogPriceListRowsResponse>>
        GetRows(
            Guid priceListId,
            [FromQuery]
            CatalogPriceListRowStatus? status,
            [FromQuery]
            CatalogPriceListRowMatchStatus? matchStatus,
            [FromQuery]
            string? issueCode,
            [FromQuery]
            int page = 1,
            [FromQuery]
            int pageSize = 50,
            [FromServices]
            ICurrentUserProvider currentUserProvider = null!,
            [FromServices]
            GetCatalogPriceListRowsQueryHandler handler = null!,
            CancellationToken cancellationToken = default)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogPriceListRowsQuery(
                priceListId,
                currentUserProvider.UserId.Value,
                status,
                matchStatus,
                issueCode,
                page,
                pageSize);

        var result =
            await handler
                .Handle(
                    query,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(
                result.Error,
                ProblemTitle);
        }

        var items =
            result.Value.Items
                .Select(
                    row =>
                        new GetCatalogPriceListRowResponse(
                            row.RowId,
                            row.RowNumber,
                            row.Article,
                            row.Name,
                            row.BasePriceAmount,
                            row.MrcPriceAmount,
                            row.ProductUrl,
                            row.Unit,
                            row.ProductId,
                            row.Status.ToString(),
                            row.MatchStatus.ToString(),
                            row.MatchConfidencePercent,
                            row.Issues
                                .Select(
                                    issue =>
                                        new CatalogPriceListRowIssueResponse(
                                            issue.Code,
                                            issue.Field,
                                            issue.Message))
                                .ToArray()))
                .ToArray();

        var totalPages =
            result.Value.TotalCount == 0
                ? 0
                : (result.Value.TotalCount
                    + pageSize
                    - 1)
                  / pageSize;

        return Ok(
            new GetCatalogPriceListRowsResponse(
                page,
                pageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}