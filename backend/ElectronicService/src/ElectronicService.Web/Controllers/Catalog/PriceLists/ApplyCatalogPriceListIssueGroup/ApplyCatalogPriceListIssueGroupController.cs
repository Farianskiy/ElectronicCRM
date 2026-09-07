using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.ApplyCatalogPriceListIssueGroup;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.ApplyCatalogPriceListIssueGroup;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class ApplyCatalogPriceListIssueGroupController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось применить групповое исправление прайс-листа.";

    [HttpPut(
        "{priceListId:guid}/issue-groups/{groupKey}")]
    [ProducesResponseType(
        typeof(ApplyCatalogPriceListIssueGroupResponse),
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
        ActionResult<ApplyCatalogPriceListIssueGroupResponse>>
        Apply(
            Guid priceListId,
            string groupKey,
            ApplyCatalogPriceListIssueGroupRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ApplyCatalogPriceListIssueGroupCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new ApplyCatalogPriceListIssueGroupCommand(
                priceListId,
                currentUserProvider.UserId.Value,
                groupKey,
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

        return Ok(
            new ApplyCatalogPriceListIssueGroupResponse(
                result.Value.PriceListId,
                result.Value.PriceListStatus.ToString(),
                result.Value.ProcessedRowsCount,
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount));
    }
}