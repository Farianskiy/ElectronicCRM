using ElectronicService.Contracts.Catalog
    .CharacteristicDefinitions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.GetDefinitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .CharacteristicDefinitions.GetDefinitions;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/characteristic-definitions")]
public sealed class
    CatalogCharacteristicDefinitionsController
    : ControllerBase
{
    private readonly
        GetCatalogCharacteristicDefinitionsQueryHandler
        _handler;

    public CatalogCharacteristicDefinitionsController(
        GetCatalogCharacteristicDefinitionsQueryHandler
            handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(
            IReadOnlyCollection<
                CatalogCharacteristicDefinitionResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        IReadOnlyCollection<
            CatalogCharacteristicDefinitionResponse>>>
        GetDefinitions(
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(
                new GetCatalogCharacteristicDefinitionsQuery(
                    search),
                cancellationToken)
            .ConfigureAwait(false);

        var response = result
            .Select(definition =>
                new CatalogCharacteristicDefinitionResponse(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType,
                    definition.Unit,
                    definition.ProductTypesCount,
                    definition.ProductsWithValueCount))
            .ToList();

        return Ok(response);
    }
}