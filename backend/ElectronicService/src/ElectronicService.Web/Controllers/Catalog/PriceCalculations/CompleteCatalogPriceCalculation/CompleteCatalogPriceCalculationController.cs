using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class CompleteCatalogPriceCalculationController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось завершить расчёт цен.";

    [HttpPost("{calculationId:guid}/complete")]
    [ProducesResponseType(
        typeof(CompleteCatalogPriceCalculationResponse),
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
        CompleteCatalogPriceCalculationResponse>>
        Complete(
            Guid calculationId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            CompleteCatalogPriceCalculationCommandHandler
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
            new CompleteCatalogPriceCalculationCommand(
                calculationId,
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
            new CompleteCatalogPriceCalculationResponse(
                value.CalculationId,
                value.Title,
                value.Currency,
                value.Status.ToString(),
                value.LinesCount,
                value.ManufacturersCount,
                value.TotalAmount,
                value.CompletedAtUtc));
    }
}