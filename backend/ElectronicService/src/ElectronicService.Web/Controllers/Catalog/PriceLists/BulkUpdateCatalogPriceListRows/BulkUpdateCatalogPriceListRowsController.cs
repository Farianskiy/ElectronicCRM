using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.BulkUpdateCatalogPriceListRows;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.BulkUpdateCatalogPriceListRows;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class BulkUpdateCatalogPriceListRowsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось сохранить выбранные строки прайс-листа.";

    [HttpPut("{priceListId:guid}/rows/bulk")]
    [ProducesResponseType(
        typeof(BulkUpdateCatalogPriceListRowsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<BulkUpdateCatalogPriceListRowsResponse>>
        Update(
            Guid priceListId,
            BulkUpdateCatalogPriceListRowsRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            BulkUpdateCatalogPriceListRowsCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var requestRows =
            request.Rows
            ?? [];

        var commandRows =
            requestRows
                .Select(
                    row =>
                        new BulkUpdateCatalogPriceListRowItem(
                            row.RowId,
                            row.Article,
                            row.Name,
                            row.BasePriceAmount,
                            row.MrcPriceAmount,
                            row.ProductUrl,
                            row.Unit,
                            row.ProductId))
                .ToArray();

        var command =
            new BulkUpdateCatalogPriceListRowsCommand(
                priceListId,
                currentUserProvider.UserId.Value,
                commandRows);

        var result =
            await handler
                .Handle(
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(
                result.Error,
                ProblemTitle);
        }

        var responseRows =
            result.Value.Rows
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

        return Ok(
            new BulkUpdateCatalogPriceListRowsResponse(
                result.Value.PriceListId,
                result.Value.PriceListStatus.ToString(),
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount,
                responseRows));
    }
}