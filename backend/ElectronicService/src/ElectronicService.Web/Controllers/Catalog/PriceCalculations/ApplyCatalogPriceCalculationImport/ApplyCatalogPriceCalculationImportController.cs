using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class ApplyCatalogPriceCalculationImportController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось добавить позиции из файла в проект.";

    [HttpPost("{calculationId:guid}/import-apply")]
    [ProducesResponseType(
        typeof(ApplyCatalogPriceCalculationImportResponse),
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
        ApplyCatalogPriceCalculationImportResponse>> Apply(
            Guid calculationId,
            [FromBody]
            ApplyCatalogPriceCalculationImportRequest request,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            ApplyCatalogPriceCalculationImportCommandHandler handler,
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

        if (request.Rows is null || request.Rows.Count == 0)
        {
            return Problem(
                detail:
                    "Нет сопоставленных строк для добавления.",
                statusCode:
                    StatusCodes.Status400BadRequest,
                title: ProblemTitle);
        }

        var command =
            new ApplyCatalogPriceCalculationImportCommand(
                calculationId,
                request.Rows
                    .Select(
                        row =>
                            new ApplyCatalogPriceCalculationImportRow(
                                row.ProductId,
                                row.Quantity))
                    .ToArray(),
                currentUserProvider.UserId.Value);

        var result =
            await handler
                .Handle(command, cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(
                result.Error,
                ProblemTitle);
        }

        return Ok(
            new ApplyCatalogPriceCalculationImportResponse(
                result.Value.CalculationId,
                result.Value.AddedLinesCount,
                result.Value.CalculationTotalAmount));
    }
}
