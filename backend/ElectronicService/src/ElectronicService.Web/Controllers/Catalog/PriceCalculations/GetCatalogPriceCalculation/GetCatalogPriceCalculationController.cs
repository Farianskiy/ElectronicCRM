using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.GetCatalogPriceCalculation;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.GetCatalogPriceCalculation;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class GetCatalogPriceCalculationController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить расчёт цен.";

    [HttpGet("{calculationId:guid}")]
    [ProducesResponseType(
        typeof(GetCatalogPriceCalculationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<GetCatalogPriceCalculationResponse>>
        Get(
            Guid calculationId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            GetCatalogPriceCalculationQueryHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogPriceCalculationQuery(
                calculationId,
                currentUserProvider.UserId.Value);

        var result =
            await handler
                .Handle(
                    query,
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
                        new CatalogPriceCalculationLineResponse(
                            line.LineId,
                            line.ProductId,
                            line.ManufacturerId,
                            line.ManufacturerName,
                            line.PriceListId,
                            line.PriceListRowId,
                            line.Article,
                            line.Name,
                            line.Unit,
                            line.Quantity,
                            line.BasePriceAmount,
                            line.MrcPriceAmount,
                            line.DiscountPercent,
                            line.ProjectPriceAmount,
                            line.TotalAmount,
                            line.CreatedAtUtc,
                            line.UpdatedAtUtc))
                .ToArray();

        var discounts =
            value.ManufacturerDiscounts
                .Select(
                    discount =>
                        new CatalogPriceCalculationDiscountResponse(
                            discount.DiscountId,
                            discount.ManufacturerId,
                            discount.ManufacturerName,
                            discount.DiscountPercent,
                            discount.CreatedAtUtc,
                            discount.UpdatedAtUtc))
                .ToArray();

        return Ok(
            new GetCatalogPriceCalculationResponse(
                value.CalculationId,
                value.CreatedByUserId,
                value.Title,
                value.Currency,
                value.Status.ToString(),
                value.TotalAmount,
                value.CreatedAtUtc,
                value.UpdatedAtUtc,
                value.CompletedAtUtc,
                value.ArchivedAtUtc,
                lines,
                discounts));
    }
}