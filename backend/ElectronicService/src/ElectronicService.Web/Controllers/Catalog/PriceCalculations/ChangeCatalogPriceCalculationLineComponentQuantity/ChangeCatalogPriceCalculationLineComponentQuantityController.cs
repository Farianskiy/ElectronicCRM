using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentQuantity;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentQuantity;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceCalculationsManage)]
[Route("api/catalog/price-calculations")]
public sealed class ChangeCatalogPriceCalculationLineComponentQuantityController : ControllerBase
{
    private const string ProblemTitle = "Не удалось изменить количество комплектующего.";

    [HttpPatch("{calculationId:guid}/lines/{lineId:guid}/components/{componentLineId:guid}/quantity")]
    [ProducesResponseType(typeof(ChangeCatalogPriceCalculationLineComponentQuantityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChangeCatalogPriceCalculationLineComponentQuantityResponse>> ChangeQuantity(
        Guid calculationId,
        Guid lineId,
        Guid componentLineId,
        [FromBody] ChangeCatalogPriceCalculationLineComponentQuantityRequest request,
        [FromServices] ICurrentUserProvider currentUserProvider,
        [FromServices] ChangeCatalogPriceCalculationLineComponentQuantityCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command = new ChangeCatalogPriceCalculationLineComponentQuantityCommand(
            calculationId,
            lineId,
            componentLineId,
            request.QuantityPerUnit,
            currentUserProvider.UserId.Value);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;

        return Ok(new ChangeCatalogPriceCalculationLineComponentQuantityResponse(
            value.CalculationId,
            value.LineId,
            value.ComponentLineId,
            value.QuantityPerUnit,
            value.TotalQuantity,
            value.BasePriceAmount,
            value.DiscountPercent,
            value.ProjectPriceAmount,
            value.ComponentTotalAmount,
            value.LineTotalAmount,
            value.CalculationTotalAmount));
    }
}
