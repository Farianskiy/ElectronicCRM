using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.SetCatalogPriceCalculationManufacturerDiscount;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.SetCatalogPriceCalculationManufacturerDiscount;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class SetCatalogPriceCalculationManufacturerDiscountController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось установить скидку производителя.";

    [HttpPut(
        "{calculationId:guid}/discounts/"
        + "{manufacturerId:guid}")]
    [ProducesResponseType(
        typeof(CatalogPriceCalculationDiscountChangeResponse),
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
        CatalogPriceCalculationDiscountChangeResponse>>
        Set(
            Guid calculationId,
            Guid manufacturerId,
            [FromBody]
            SetCatalogPriceCalculationManufacturerDiscountRequest
                request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            SetCatalogPriceCalculationManufacturerDiscountCommandHandler
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
            new SetCatalogPriceCalculationManufacturerDiscountCommand(
                calculationId,
                manufacturerId,
                request.DiscountPercent,
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

        var lines =
            value.Lines
                .Select(
                    line =>
                        new CatalogPriceCalculationRecalculatedLineResponse(
                            line.LineId,
                            line.ProductId,
                            line.Quantity,
                            line.BasePriceAmount,
                            line.DiscountPercent,
                            line.ProjectPriceAmount,
                            line.TotalAmount))
                .ToArray();

        return Ok(
            new CatalogPriceCalculationDiscountChangeResponse(
                value.CalculationId,
                value.ManufacturerId,
                value.ManufacturerName,
                value.DiscountPercent,
                value.AffectedLinesCount,
                value.CalculationTotalAmount,
                lines));
    }
}