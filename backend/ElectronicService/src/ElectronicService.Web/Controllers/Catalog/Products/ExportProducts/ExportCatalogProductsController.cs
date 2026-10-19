using ElectronicService.Contracts.Catalog.Products.Search;
using ElectronicService.Core.Catalog.Products.ExportProducts;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/export")]
[PermissionAuthorize(UserPermissionCode.ProductsView)]
public sealed class ExportCatalogProductsController : ControllerBase
{
    [HttpPost]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromBody] SearchProductsRequest request,
        [FromServices] ExportCatalogProductsQueryHandler handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        if ((request.IncludeArchived || request.OnlyArchived)
            && !User.HasClaim(
                PermissionClaimTypes.Permission,
                UserPermissionCode.ProductsEdit.ToString()))
        {
            return Forbid();
        }

        var characteristicFilters = request.Characteristics?
            .Select(characteristic =>
                new SearchProductCharacteristicFilter(
                    characteristic.Code,
                    characteristic.Value))
            .ToList()
            ?? [];

        var filter = new SearchProductsQuery(
            Search: request.Search,
            ProductTypeCode: request.ProductTypeCode,
            Manufacturer: request.Manufacturer,
            Characteristics: characteristicFilters,
            Page: 1,
            PageSize: 100,
            OnlyInStock: request.OnlyInStock,
            IncludeArchived: request.IncludeArchived,
            OnlyArchived: request.OnlyArchived);

        var result = await handler.Handle(filter, cancellationToken)
            .ConfigureAwait(false);

        Response.Headers.Append(
            "X-Exported-Products-Count",
            result.ProductsCount.ToString(
                System.Globalization.CultureInfo.InvariantCulture));

        return File(
            result.Content.ToArray(),
            result.ContentType,
            result.FileName);
    }
}
