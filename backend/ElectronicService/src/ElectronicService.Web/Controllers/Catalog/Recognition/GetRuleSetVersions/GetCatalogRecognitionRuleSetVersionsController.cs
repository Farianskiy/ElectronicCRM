using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetRuleSetVersions;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionRuleSetVersionsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionRuleSetVersionPage>> GetPage(
        [FromServices] ICatalogRecognitionRuleSetVersionReader reader,
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите производителя и тип товара."
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

        var result = await reader.GetPageAsync(
            manufacturerId,
            productTypeId,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        return Ok(result);
    }

    [HttpGet("{versionId:guid}")]
    public async Task<ActionResult<CatalogRecognitionRuleSetVersionDetails>> GetById(
        [FromRoute] Guid versionId,
        [FromServices] ICatalogRecognitionRuleSetVersionReader reader,
        CancellationToken cancellationToken)
    {
        if (versionId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите идентификатор версии."
            });
        }

        var result = await reader.GetByIdAsync(versionId, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Версия правил не найдена."
            });
        }

        return Ok(result);
    }
}