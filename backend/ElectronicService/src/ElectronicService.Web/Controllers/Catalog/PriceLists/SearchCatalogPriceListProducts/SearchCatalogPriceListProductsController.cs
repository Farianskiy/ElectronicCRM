using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.SearchCatalogPriceListProducts;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.SearchCatalogPriceListProducts;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class SearchCatalogPriceListProductsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось найти товары производителя.";

    [HttpGet("{priceListId:guid}/products")]
    [ProducesResponseType(
        typeof(SearchCatalogPriceListProductsResponse),
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
        ActionResult<SearchCatalogPriceListProductsResponse>>
        Search(
            Guid priceListId,
            [FromQuery]
            string? search,
            [FromQuery]
            int page = 1,
            [FromQuery]
            int pageSize = 20,
            [FromServices]
            ICurrentUserProvider currentUserProvider = null!,
            [FromServices]
            SearchCatalogPriceListProductsQueryHandler handler = null!,
            CancellationToken cancellationToken = default)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new SearchCatalogPriceListProductsQuery(
                priceListId,
                currentUserProvider.UserId.Value,
                search,
                page,
                pageSize);

        var result =
            await handler
                .Handle(
                    query,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(
                result.Error,
                ProblemTitle);
        }

        var items =
            result.Value.Items
                .Select(
                    product =>
                        new CatalogPriceListProductSearchResponse(
                            product.ProductId,
                            product.Article,
                            product.Name,
                            product.ProductTypeCode,
                            product.ProductTypeName))
                .ToArray();

        var totalPages =
            result.Value.TotalCount == 0
                ? 0
                : (result.Value.TotalCount
                    + pageSize
                    - 1)
                  / pageSize;

        return Ok(
            new SearchCatalogPriceListProductsResponse(
                page,
                pageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}