using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLineComponent;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.AddCatalogPriceCalculationLineComponent;

[ApiController]
[PermissionAuthorize(UserPermissionCode.PriceCalculationsManage)]
[Route("api/catalog/price-calculations")]
public sealed class AddCatalogPriceCalculationLineComponentController : ControllerBase
{
    private const string ProblemTitle = "Не удалось добавить комплектующее в расчёт.";

    [HttpPost("{calculationId:guid}/lines/{lineId:guid}/components")]
    [ProducesResponseType(typeof(AddCatalogPriceCalculationLineComponentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddCatalogPriceCalculationLineComponentResponse>> Add(
        Guid calculationId,
        Guid lineId,
        [FromBody] AddCatalogPriceCalculationLineComponentRequest request,
        [FromServices] ICurrentUserProvider currentUserProvider,
        [FromServices] AddCatalogPriceCalculationLineComponentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command = new AddCatalogPriceCalculationLineComponentCommand(
            calculationId,
            lineId,
            request.NeedDefinitionId,
            request.ComponentProductId,
            request.QuantityPerUnit,
            request.ManualSelection,
            currentUserProvider.UserId.Value);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;
        var response = new AddCatalogPriceCalculationLineComponentResponse(
            value.CalculationId,
            value.LineId,
            value.ComponentLineId,
            value.NeedDefinitionId,
            value.NeedName,
            value.ComponentProductId,
            value.ManufacturerId,
            value.ManufacturerName,
            value.Article,
            value.Name,
            value.SelectionSource.ToString(),
            value.QuantityPerUnit,
            value.BasePriceAmount,
            value.DiscountPercent,
            value.ProjectPriceAmount,
            value.TotalQuantity,
            value.ComponentTotalAmount,
            value.LineTotalAmount,
            value.CalculationTotalAmount);

        return Created(
            new Uri(
                $"/api/catalog/price-calculations/{value.CalculationId}/lines/{value.LineId}/components/{value.ComponentLineId}",
                UriKind.Relative),
            response);
    }
}
