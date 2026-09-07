using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineQuantity;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineQuantity;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class ChangeCatalogPriceCalculationLineQuantityController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось изменить количество товара.";

    [HttpPatch(
        "{calculationId:guid}/lines/"
        + "{lineId:guid}/quantity")]
    [ProducesResponseType(
        typeof(
            ChangeCatalogPriceCalculationLineQuantityResponse),
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
    public async Task<ActionResult<
        ChangeCatalogPriceCalculationLineQuantityResponse>>
        ChangeQuantity(
            Guid calculationId,
            Guid lineId,
            [FromBody]
            ChangeCatalogPriceCalculationLineQuantityRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ChangeCatalogPriceCalculationLineQuantityCommandHandler
                handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new ChangeCatalogPriceCalculationLineQuantityCommand(
                calculationId,
                lineId,
                request.Quantity,
                currentUserProvider.UserId.Value);

        var result =
            await handler
                .Handle(
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(
                result.Error,
                ProblemTitle);
        }

        var value =
            result.Value;

        return Ok(
            new ChangeCatalogPriceCalculationLineQuantityResponse(
                value.CalculationId,
                value.LineId,
                value.Quantity,
                value.BasePriceAmount,
                value.DiscountPercent,
                value.ProjectPriceAmount,
                value.LineTotalAmount,
                value.CalculationTotalAmount));
    }
}