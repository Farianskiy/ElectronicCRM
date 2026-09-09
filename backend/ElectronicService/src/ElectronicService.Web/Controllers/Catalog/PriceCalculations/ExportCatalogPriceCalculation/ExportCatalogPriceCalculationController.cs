using ElectronicService.Core.Catalog.PriceCalculations.ExportCatalogPriceCalculation;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.ExportCatalogPriceCalculation;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/price-calculations")]
public sealed class ExportCatalogPriceCalculationController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выгрузить расчёт.";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("{calculationId:guid}/export")]
    [Produces(ExcelContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid calculationId, [FromServices] ExportCatalogPriceCalculationQueryHandler handler, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var result = await handler.Handle(
            new ExportCatalogPriceCalculationQuery(calculationId, currentUserId),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceCalculationProblem(result.Error, ProblemTitle);
        }

        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(result.Value.Content.ToArray(), result.Value.ContentType, result.Value.FileName);
    }
}