using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.CreateCatalogPriceCalculation;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.CreateCatalogPriceCalculation;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class CreateCatalogPriceCalculationController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось создать расчёт цен.";

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateCatalogPriceCalculationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<CreateCatalogPriceCalculationResponse>>
        Create(
            [FromBody]
            CreateCatalogPriceCalculationRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            CreateCatalogPriceCalculationCommandHandler handler,
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
            new CreateCatalogPriceCalculationCommand(
                currentUserProvider.UserId.Value,
                request.Title);

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
            new CreateCatalogPriceCalculationResponse(
                value.CalculationId,
                value.CreatedByUserId,
                value.Title,
                value.Currency,
                value.Status.ToString(),
                value.TotalAmount,
                value.CreatedAtUtc);

        return Created(
            new Uri(
                $"/api/catalog/price-calculations/"
                + $"{value.CalculationId}",
                UriKind.Relative),
            response);
    }
}