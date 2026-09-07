using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListIssueGroups;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.GetCatalogPriceListIssueGroups;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class GetCatalogPriceListIssueGroupsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить группы ошибок прайс-листа.";

    [HttpGet("{priceListId:guid}/issue-groups")]
    [ProducesResponseType(
        typeof(GetCatalogPriceListIssueGroupsResponse),
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
        ActionResult<GetCatalogPriceListIssueGroupsResponse>>
        GetGroups(
            Guid priceListId,
            [FromQuery]
            string? issueCode,
            [FromQuery]
            int page = 1,
            [FromQuery]
            int pageSize = 50,
            [FromServices]
            ICurrentUserProvider currentUserProvider = null!,
            [FromServices]
            GetCatalogPriceListIssueGroupsQueryHandler handler = null!,
            CancellationToken cancellationToken = default)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogPriceListIssueGroupsQuery(
                priceListId,
                currentUserProvider.UserId.Value,
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
                    group =>
                        new CatalogPriceListIssueGroupResponse(
                            group.GroupKey,
                            group.IssueCode,
                            group.Field,
                            group.SourceValue,
                            group.RowsCount,
                            group.ExampleRowNumbers))
                .ToArray();

        var totalPages =
            result.Value.TotalCount == 0
                ? 0
                : (result.Value.TotalCount
                    + pageSize
                    - 1)
                  / pageSize;

        return Ok(
            new GetCatalogPriceListIssueGroupsResponse(
                page,
                pageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}