using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLine;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.AddCatalogPriceCalculationLine;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class AddCatalogPriceCalculationLineController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось добавить товар в расчёт.";

    [HttpPost("{calculationId:guid}/lines")]
    [ProducesResponseType(
        typeof(AddCatalogPriceCalculationLineResponse),
        StatusCodes.Status201Created)]
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
    public async Task<
        ActionResult<AddCatalogPriceCalculationLineResponse>>
        Add(
            Guid calculationId,
            [FromBody]
            AddCatalogPriceCalculationLineRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            AddCatalogPriceCalculationLineCommandHandler handler,
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
            new AddCatalogPriceCalculationLineCommand(
                calculationId,
                request.ProductId,
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

        var response =
            new AddCatalogPriceCalculationLineResponse(
                value.CalculationId,
                value.LineId,
                value.ProductId,
                value.ManufacturerId,
                value.ManufacturerName,
                value.PriceListId,
                value.PriceListRowId,
                value.Article,
                value.Name,
                value.Unit,
                value.Quantity,
                value.BasePriceAmount,
                value.MrcPriceAmount,
                value.DiscountPercent,
                value.ProjectPriceAmount,
                value.LineTotalAmount,
                value.CalculationTotalAmount);

        return Created(
            new Uri(
                $"/api/catalog/price-calculations/"
                + $"{value.CalculationId}/lines/"
                + $"{value.LineId}",
                UriKind.Relative),
            response);
    }
}