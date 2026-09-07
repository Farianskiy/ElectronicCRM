using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.ProcessCatalogPriceList;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.ProcessCatalogPriceList;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class ProcessCatalogPriceListController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось обработать прайс-лист.";

    [HttpPost("{priceListId:guid}/process")]
    [ProducesResponseType(
        typeof(ProcessCatalogPriceListResponse),
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
        ActionResult<ProcessCatalogPriceListResponse>>
        Process(
            Guid priceListId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ProcessCatalogPriceListCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);

        ArgumentNullException.ThrowIfNull(
            handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new ProcessCatalogPriceListCommand(
                priceListId,
                currentUserProvider.UserId.Value);

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

        var response =
            new ProcessCatalogPriceListResponse(
                result.Value.PriceListId,
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount,
                result.Value.Status.ToString());

        return Ok(response);
    }
}