using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetIntegerDrafts;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/integer-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionIntegerDraftsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionIntegerDraftPage>> GetPage(
        [FromServices] ICatalogRecognitionIntegerDraftReader reader,
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] Guid characteristicDefinitionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty || characteristicDefinitionId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите производителя, тип товара и характеристику."
            });
        }

        if (page < 1 || page > 10000 || pageSize < 1 || pageSize > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Номер страницы должен быть от 1 до 10000, размер страницы — от 1 до 50."
            });
        }

        var scope = new CatalogRecognitionTrainingScope(manufacturerId, productTypeId, characteristicDefinitionId);
        var result = await reader.GetPageAsync(scope, page, pageSize, cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }
}