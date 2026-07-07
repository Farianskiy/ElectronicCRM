using ElectronicService.Contracts.Catalog.Metadata;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Metadata.ProductTypes;

[ApiController]
[Route("api/catalog/metadata/product-types")]
public sealed class CatalogProductTypesController : ControllerBase
{
    private readonly GetCatalogProductTypesQueryHandler _handler;

    public CatalogProductTypesController(
        GetCatalogProductTypesQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CatalogProductTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogProductTypeResponse>>> GetProductTypes(
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(new GetCatalogProductTypesQuery(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(result.Select(productType => new CatalogProductTypeResponse(
            productType.Id,
            productType.Code,
            productType.Name)).ToList());
    }
}