using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.ActivateCatalogPriceList;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.ActivateCatalogPriceList;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class ActivateCatalogPriceListController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось активировать прайс-лист.";

    [HttpPost("{priceListId:guid}/activate")]
    [ProducesResponseType(
        typeof(ActivateCatalogPriceListResponse),
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
        ActionResult<ActivateCatalogPriceListResponse>>
        Activate(
            Guid priceListId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ActivateCatalogPriceListCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);

        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new ActivateCatalogPriceListCommand(
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

        var value =
            result.Value;

        return Ok(
            new ActivateCatalogPriceListResponse(
                value.PriceListId,
                value.ManufacturerId,
                value.Status.ToString(),
                value.ActivatedAtUtc,
                value.ArchivedPriceListId));
    }
}