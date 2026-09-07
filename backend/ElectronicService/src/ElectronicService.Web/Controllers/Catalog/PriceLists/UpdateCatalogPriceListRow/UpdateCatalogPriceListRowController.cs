using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.UpdateCatalogPriceListRow;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.UpdateCatalogPriceListRow;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class UpdateCatalogPriceListRowController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось изменить строку прайс-листа.";

    [HttpPut("{priceListId:guid}/rows/{rowId:guid}")]
    [ProducesResponseType(
        typeof(UpdateCatalogPriceListRowResponse),
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
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<
        ActionResult<UpdateCatalogPriceListRowResponse>>
        Update(
            Guid priceListId,
            Guid rowId,
            UpdateCatalogPriceListRowRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            UpdateCatalogPriceListRowCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new UpdateCatalogPriceListRowCommand(
                priceListId,
                rowId,
                currentUserProvider.UserId.Value,
                request.Article,
                request.Name,
                request.BasePriceAmount,
                request.MrcPriceAmount,
                request.ProductUrl,
                request.Unit,
                request.ProductId);

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

        var row =
            result.Value.Row;

        var rowResponse =
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
                    .ToArray());

        return Ok(
            new UpdateCatalogPriceListRowResponse(
                result.Value.PriceListId,
                result.Value.PriceListStatus.ToString(),
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount,
                rowResponse));
    }
}