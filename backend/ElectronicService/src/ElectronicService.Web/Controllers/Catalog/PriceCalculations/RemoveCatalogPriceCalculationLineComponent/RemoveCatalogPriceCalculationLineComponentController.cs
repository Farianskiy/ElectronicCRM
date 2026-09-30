using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLineComponent;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLineComponent;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceCalculationsManage)]
[Route("api/catalog/price-calculations")]
public sealed class RemoveCatalogPriceCalculationLineComponentController : ControllerBase
{
    private const string ProblemTitle = "Не удалось удалить комплектующее из расчёта.";

    [HttpDelete("{calculationId:guid}/lines/{lineId:guid}/components/{componentLineId:guid}")]
    [ProducesResponseType(typeof(RemoveCatalogPriceCalculationLineComponentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemoveCatalogPriceCalculationLineComponentResponse>> Remove(
        Guid calculationId,
        Guid lineId,
        Guid componentLineId,
        [FromServices] ICurrentUserProvider currentUserProvider,
        [FromServices] RemoveCatalogPriceCalculationLineComponentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command = new RemoveCatalogPriceCalculationLineComponentCommand(
            calculationId,
            lineId,
            componentLineId,
            currentUserProvider.UserId.Value);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;

        return Ok(new RemoveCatalogPriceCalculationLineComponentResponse(
            value.CalculationId,
            value.LineId,
            value.RemovedComponentLineId,
            value.RemainingComponentsCount,
            value.LineTotalAmount,
            value.CalculationTotalAmount));
    }
}
