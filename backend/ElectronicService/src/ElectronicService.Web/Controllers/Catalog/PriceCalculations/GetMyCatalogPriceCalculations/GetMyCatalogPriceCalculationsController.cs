using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.GetMyCatalogPriceCalculations;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.GetMyCatalogPriceCalculations;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class GetMyCatalogPriceCalculationsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить список расчётов цен.";

    [HttpGet]
    [ProducesResponseType(
        typeof(GetMyCatalogPriceCalculationsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<GetMyCatalogPriceCalculationsResponse>>
        Get(
            [FromQuery]
            CatalogPriceCalculationStatus? status,
            [FromQuery]
            int page,
            [FromQuery]
            int pageSize,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            GetMyCatalogPriceCalculationsQueryHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);
        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var actualPage =
            page <= 0
                ? 1
                : page;

        var actualPageSize =
            pageSize <= 0
                ? 20
                : pageSize;

        var query =
            new GetMyCatalogPriceCalculationsQuery(
                currentUserProvider.UserId.Value,
                status,
                actualPage,
                actualPageSize);

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

        var totalPages =
            result.Value.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    result.Value.TotalCount
                    / (double)actualPageSize);

        var items =
            result.Value.Items
                .Select(
                    item =>
                        new CatalogPriceCalculationListItemResponse(
                            item.CalculationId,
                            item.Title,
                            item.Currency,
                            item.Status.ToString(),
                            item.TotalAmount,
                            item.LinesCount,
                            item.ManufacturersCount,
                            item.CreatedAtUtc,
                            item.UpdatedAtUtc,
                            item.CompletedAtUtc,
                            item.ArchivedAtUtc))
                .ToArray();

        return Ok(
            new GetMyCatalogPriceCalculationsResponse(
                actualPage,
                actualPageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}