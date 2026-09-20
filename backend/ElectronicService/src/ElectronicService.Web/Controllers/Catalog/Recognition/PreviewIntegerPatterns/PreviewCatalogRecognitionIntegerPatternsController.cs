using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewIntegerPatterns;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/integer-pattern-proposals")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionIntegerPatternsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionIntegerAlternativesProposalSet>> Get(
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] Guid characteristicDefinitionId,
        [FromServices] ICatalogProductMetadataRepository metadataRepository,
        [FromServices] ICharacteristicDefinitionRepository definitionRepository,
        [FromServices] CatalogRecognitionTrainingDataLoader loader,
        CancellationToken cancellationToken)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty || characteristicDefinitionId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = "Укажите производителя, тип товара и характеристику." });
        }

        var manufacturer = await metadataRepository.GetManufacturerByIdAsync(manufacturerId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = "Производитель не найден." });
        }

        var productType = await metadataRepository.GetProductTypeByIdAsync(productTypeId, cancellationToken).ConfigureAwait(false);

        if (productType is null)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = "Тип товара не найден." });
        }

        if (!productType.Characteristics.Any(item => item.CharacteristicDefinitionId == characteristicDefinitionId))
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = "Характеристика не относится к выбранному типу товара." });
        }

        var definition = await definitionRepository.GetByIdAsync(characteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (definition is null)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = "Характеристика не найдена." });
        }

        if (definition.DataType != CharacteristicDataType.Number)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = "Этот генератор предназначен только для числовых характеристик. Выберите, например, номинальный ток." });
        }

        var scope = new CatalogRecognitionTrainingScope(manufacturerId, productTypeId, characteristicDefinitionId);
        var prepared = await loader.LoadAsync(scope, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return Ok(CatalogRecognitionIntegerAlternativesGenerator.Generate(prepared));
    }
}