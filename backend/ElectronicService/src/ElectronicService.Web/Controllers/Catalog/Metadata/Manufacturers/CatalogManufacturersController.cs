using ElectronicService.Contracts.Catalog.Metadata;
using ElectronicService.Core.Catalog.Metadata.GetManufacturers;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Metadata.Manufacturers;

[ApiController]
[Route("api/catalog/metadata/manufacturers")]
public sealed class CatalogManufacturersController : ControllerBase
{
    private readonly GetCatalogManufacturersQueryHandler _handler;

    public CatalogManufacturersController(
        GetCatalogManufacturersQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CatalogManufacturerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogManufacturerResponse>>> GetManufacturers(
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(
                new GetCatalogManufacturersQuery(search),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result.Select(manufacturer => new CatalogManufacturerResponse(
            manufacturer.Id,
            manufacturer.Name)).ToList());
    }
}