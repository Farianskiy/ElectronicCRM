using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentSupplyStatus;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentSupplyStatus;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceCalculationsManage)]
[Route("api/catalog/price-calculations")]
public sealed class ChangeCatalogPriceCalculationLineComponentSupplyStatusController
    : ControllerBase
{
    [HttpPatch("{calculationId:guid}/lines/{lineId:guid}/components/{componentLineId:guid}/supply-status")]
    public async Task<ActionResult<
        ChangeCatalogPriceCalculationLineComponentSupplyStatusResponse>> Change(
            Guid calculationId,
            Guid lineId,
            Guid componentLineId,
            [FromBody]
            ChangeCatalogPriceCalculationLineComponentSupplyStatusRequest request,
            [FromServices] ICurrentUserProvider currentUserProvider,
            [FromServices]
            ChangeCatalogPriceCalculationLineComponentSupplyStatusCommandHandler handler,
            CancellationToken cancellationToken)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        if (!Enum.TryParse<CatalogPriceCalculationLineComponentSupplyStatus>(
                request.SupplyStatus,
                true,
                out var supplyStatus)
            || !Enum.IsDefined(supplyStatus))
        {
            return BadRequest(
                "Способ обеспечения должен быть ToPurchase, AlreadyAvailable или IncludedInSupply.");
        }

        var result = await handler.Handle(
            new ChangeCatalogPriceCalculationLineComponentSupplyStatusCommand(
                calculationId,
                lineId,
                componentLineId,
                supplyStatus,
                currentUserProvider.UserId.Value),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(
                result.Error,
                "Не удалось изменить способ обеспечения комплектующего.");
        }

        var value = result.Value;

        return Ok(
            new ChangeCatalogPriceCalculationLineComponentSupplyStatusResponse(
                value.CalculationId,
                value.LineId,
                value.ComponentLineId,
                value.SupplyStatus.ToString(),
                value.ComponentTotalAmount,
                value.LineTotalAmount,
                value.CalculationTotalAmount));
    }
}
