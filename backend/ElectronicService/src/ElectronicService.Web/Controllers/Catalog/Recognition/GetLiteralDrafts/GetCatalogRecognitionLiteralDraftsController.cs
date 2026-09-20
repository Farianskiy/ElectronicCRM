using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetLiteralDrafts;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/literal-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionLiteralDraftsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogRecognitionLiteralDraftPage>> GetPage(
        [FromServices] ICatalogRecognitionLiteralDraftReader reader,
        [FromQuery] Guid manufacturerId,
        [FromQuery] Guid productTypeId,
        [FromQuery] Guid characteristicDefinitionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
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

    [HttpGet("{draftId:guid}/evidence")]
    public async Task<ActionResult<CatalogRecognitionIntegerDraftEvidencePage>> GetEvidence(
    Guid draftId,
    [FromServices] ICatalogRecognitionIntegerDraftReader reader,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        if (draftId == Guid.Empty || page < 1 || page > 10000 || pageSize < 1 || pageSize > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите черновик, страницу от 1 до 10000 и размер страницы от 1 до 50."
            });
        }

        var result = await reader.GetEvidenceAsync(draftId, page, pageSize, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Числовой черновик не найден."
            });
        }

        return Ok(result);
    }

    [HttpGet("{draftId:guid}")]
    public async Task<ActionResult<CatalogRecognitionLiteralDraftDetails>> GetById(
        Guid draftId,
        [FromServices] ICatalogRecognitionLiteralDraftReader reader,
        CancellationToken cancellationToken)
    {
        var result = await reader.GetByIdAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Черновик предложения не найден."
            });
        }

        return Ok(result);
    }
}