using ElectronicService.Contracts.Catalog.ProductTypes;
using ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ProductTypes.GetAvailableCharacteristicDefinitions;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/product-types/{productTypeCode}/" +
    "characteristics/available")]
public sealed class
    AvailableCharacteristicDefinitionsController
    : ControllerBase
{
    private readonly
        GetAvailableCharacteristicDefinitionsQueryHandler
        _handler;

    public AvailableCharacteristicDefinitionsController(
        GetAvailableCharacteristicDefinitionsQueryHandler
            handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(
            IReadOnlyCollection<
                AvailableCharacteristicDefinitionResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        IReadOnlyCollection<
            AvailableCharacteristicDefinitionResponse>>>
        GetAvailableDefinitions(
            string productTypeCode,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
    {
        var query =
            new GetAvailableCharacteristicDefinitionsQuery(
                productTypeCode,
                search);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(
                $"Тип товара " +
                $"'{productTypeCode}' не найден.");
        }

        var response = result
            .Select(definition =>
                new AvailableCharacteristicDefinitionResponse(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType,
                    definition.Unit))
            .ToList();

        return Ok(response);
    }
}