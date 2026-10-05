using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.ExcludeCatalogPriceListUnmatchedRows;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.ExcludeCatalogPriceListUnmatchedRows;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceListsManage)]
[Route("api/catalog/price-lists")]
public sealed class ExcludeCatalogPriceListUnmatchedRowsController : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось исключить ненайденные позиции прайс-листа.";

    [HttpDelete("{priceListId:guid}/unmatched-rows")]
    [ProducesResponseType(
        typeof(ExcludeCatalogPriceListUnmatchedRowsResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ExcludeCatalogPriceListUnmatchedRowsResponse>>
        Exclude(
            Guid priceListId,
            [FromServices] ICurrentUserProvider currentUserProvider,
            [FromServices] ExcludeCatalogPriceListUnmatchedRowsCommandHandler handler,
            CancellationToken cancellationToken)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var result =
            await handler
                .Handle(
                    new ExcludeCatalogPriceListUnmatchedRowsCommand(
                        priceListId,
                        currentUserProvider.UserId.Value),
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(result.Error, ProblemTitle);
        }

        return Ok(
            new ExcludeCatalogPriceListUnmatchedRowsResponse(
                result.Value.PriceListId,
                result.Value.PriceListStatus.ToString(),
                result.Value.ExcludedRowsCount,
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount));
    }
}
