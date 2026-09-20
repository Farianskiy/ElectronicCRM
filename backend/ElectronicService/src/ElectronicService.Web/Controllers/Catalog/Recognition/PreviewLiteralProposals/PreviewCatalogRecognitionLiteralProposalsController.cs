using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewLiteralProposals;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/literal-proposals")]
public sealed class PreviewCatalogRecognitionLiteralProposalsController : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CatalogRecognitionLiteralProposalSet>> Get(
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] Guid characteristicDefinitionId,
        [FromServices] ICatalogProductMetadataRepository metadataRepository,
        [FromServices] CatalogRecognitionLiteralProposalService service,
        CancellationToken cancellationToken)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty || characteristicDefinitionId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите производителя, тип товара и характеристику."
            });
        }

        var manufacturer = await metadataRepository.GetManufacturerByIdAsync(manufacturerId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Производитель не найден."
            });
        }

        var productType = await metadataRepository.GetProductTypeByIdAsync(productTypeId, cancellationToken).ConfigureAwait(false);

        if (productType is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Тип товара не найден."
            });
        }

        if (!productType.Characteristics.Any(characteristic => characteristic.CharacteristicDefinitionId == characteristicDefinitionId))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Характеристика не относится к выбранному типу товара."
            });
        }

        var scope = new CatalogRecognitionTrainingScope(manufacturerId, productTypeId, characteristicDefinitionId);
        var result = await service.GenerateAsync(scope, cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }
}