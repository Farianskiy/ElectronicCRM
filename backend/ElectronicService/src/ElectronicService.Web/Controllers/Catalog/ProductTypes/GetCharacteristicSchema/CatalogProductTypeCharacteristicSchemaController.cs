using ElectronicService.Contracts.Catalog.ProductTypes;
using ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ProductTypes.GetCharacteristicSchema;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/product-types/{productTypeCode}/" +
    "characteristics/schema")]
public sealed class
    CatalogProductTypeCharacteristicSchemaController
    : ControllerBase
{
    private readonly
        GetCatalogProductTypeCharacteristicSchemaQueryHandler
        _handler;

    public CatalogProductTypeCharacteristicSchemaController(
        GetCatalogProductTypeCharacteristicSchemaQueryHandler
            handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(
            CatalogProductTypeCharacteristicSchemaResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<
            CatalogProductTypeCharacteristicSchemaResponse>>
        GetSchema(
            string productTypeCode,
            CancellationToken cancellationToken = default)
    {
        var query =
            new GetCatalogProductTypeCharacteristicSchemaQuery(
                productTypeCode);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(
                $"Тип товара '{productTypeCode}' не найден.");
        }

        var response =
            new CatalogProductTypeCharacteristicSchemaResponse(
                result.ProductTypeId,
                result.ProductTypeCode,
                result.ProductTypeName,
                result.ProductsCount,

                result.Characteristics
                    .Select(characteristic =>
                    {
                        var canMakeRequired =
                            characteristic
                                .ProductsWithoutValueCount
                            == 0;

                        var canRemoveFromType =
                            characteristic
                                .ProductsWithValueCount
                            == 0;

                        return new
                            CatalogProductTypeCharacteristicSchemaItemResponse(
                                characteristic.DefinitionId,
                                characteristic.Code,
                                characteristic.Name,
                                characteristic.DataType,
                                characteristic.Unit,
                                characteristic.IsRequired,
                                characteristic.IsFilterable,
                                characteristic
                                    .IsUsedForReplacement,
                                characteristic
                                    .ReplacementMatchMode,
                                characteristic
                                    .ReplacementWeight,
                                characteristic
                                    .ProductsWithValueCount,
                                characteristic
                                    .ProductsWithoutValueCount,
                                canMakeRequired,
                                canRemoveFromType);
                    })
                    .ToList());

        return Ok(response);
    }
}