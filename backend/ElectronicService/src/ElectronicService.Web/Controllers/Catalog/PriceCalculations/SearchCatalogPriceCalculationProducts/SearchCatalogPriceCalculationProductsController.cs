using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.SearchCatalogPriceCalculationProducts;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.SearchCatalogPriceCalculationProducts;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class SearchCatalogPriceCalculationProductsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось выполнить поиск товаров.";

    [HttpGet("{calculationId:guid}/products")]
    [ProducesResponseType(
        typeof(SearchCatalogPriceCalculationProductsResponse),
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
        SearchCatalogPriceCalculationProductsResponse>>
        Search(
            Guid calculationId,
            [FromQuery]
            string? search,
            [FromQuery]
            int page,
            [FromQuery]
            int pageSize,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            SearchCatalogPriceCalculationProductsQueryHandler
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

        var actualPage =
            page <= 0
                ? 1
                : page;

        var actualPageSize =
            pageSize <= 0
                ? 20
                : pageSize;

        var query =
            new SearchCatalogPriceCalculationProductsQuery(
                calculationId,
                currentUserProvider.UserId.Value,
                search,
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
                        new CatalogPriceCalculationProductSearchItemResponse(
                            item.ProductId,
                            item.ManufacturerId,
                            item.ManufacturerName,
                            item.PriceListId,
                            item.PriceListRowId,
                            item.PriceListEffectiveDate,
                            item.Article,
                            item.Name,
                            item.Unit,
                            item.BasePriceAmount,
                            item.MrcPriceAmount))
                .ToArray();

        return Ok(
            new SearchCatalogPriceCalculationProductsResponse(
                actualPage,
                actualPageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}