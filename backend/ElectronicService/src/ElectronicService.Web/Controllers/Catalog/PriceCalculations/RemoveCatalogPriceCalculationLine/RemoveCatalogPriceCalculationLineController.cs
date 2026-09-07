using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLine;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLine;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class RemoveCatalogPriceCalculationLineController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось удалить товар из расчёта.";

    [HttpDelete(
        "{calculationId:guid}/lines/{lineId:guid}")]
    [ProducesResponseType(
        typeof(RemoveCatalogPriceCalculationLineResponse),
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
        RemoveCatalogPriceCalculationLineResponse>>
        Remove(
            Guid calculationId,
            Guid lineId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            RemoveCatalogPriceCalculationLineCommandHandler
                handler,
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
            new RemoveCatalogPriceCalculationLineCommand(
                calculationId,
                lineId,
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
            new RemoveCatalogPriceCalculationLineResponse(
                value.CalculationId,
                value.RemovedLineId,
                value.RemainingLinesCount,
                value.CalculationTotalAmount));
    }
}