using ElectronicService.Contracts.Catalog.Metadata;
using ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Metadata.ProductTypeCharacteristics;

[ApiController]
[Route("api/catalog/metadata/product-types/{code}/characteristics")]
public sealed class CatalogProductTypeCharacteristicsController : ControllerBase
{
    private readonly GetCatalogProductTypeCharacteristicsQueryHandler _handler;

    public CatalogProductTypeCharacteristicsController(
        GetCatalogProductTypeCharacteristicsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CatalogProductTypeCharacteristicResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CatalogProductTypeCharacteristicResponse>>> GetCharacteristics(
        string code,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler
            .Handle(
                new GetCatalogProductTypeCharacteristicsQuery(code),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result.Select(characteristic => new CatalogProductTypeCharacteristicResponse(
            characteristic.Id,
            characteristic.Code,
            characteristic.Name,
            characteristic.DataType,
            characteristic.Unit,
            characteristic.IsRequired,
            characteristic.IsFilterable,
            characteristic.IsUsedForReplacement,
            characteristic.ReplacementMatchMode,
            characteristic.ReplacementWeight)).ToList());
    }
}