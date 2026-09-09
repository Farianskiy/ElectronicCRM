using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.UpdateCatalogPriceCalculationCard;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.UpdateCatalogPriceCalculationCard;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class UpdateCatalogPriceCalculationCardController : ControllerBase
{
    private const string ProblemTitle = "Не удалось сохранить карточку проекта.";

    [HttpPatch("{calculationId:guid}/card")]
    [ProducesResponseType(typeof(UpdateCatalogPriceCalculationCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateCatalogPriceCalculationCardResponse>> UpdateCard(
        Guid calculationId,
        [FromBody] UpdateCatalogPriceCalculationCardRequest request,
        [FromServices] ICurrentUserProvider currentUserProvider,
        [FromServices] UpdateCatalogPriceCalculationCardCommandHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var command = new UpdateCatalogPriceCalculationCardCommand(
            calculationId,
            request.CustomerName,
            request.ObjectName,
            request.ProjectNumber,
            request.ResponsibleName,
            request.Comment,
            request.ValidUntil,
            currentUserProvider.UserId.Value);

        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(result.Error, ProblemTitle);
        }

        var value = result.Value;

        return Ok(
            new UpdateCatalogPriceCalculationCardResponse(
                value.CalculationId,
                value.CustomerName,
                value.ObjectName,
                value.ProjectNumber,
                value.ResponsibleName,
                value.Comment,
                value.ValidUntil,
                value.UpdatedAtUtc));
    }
}