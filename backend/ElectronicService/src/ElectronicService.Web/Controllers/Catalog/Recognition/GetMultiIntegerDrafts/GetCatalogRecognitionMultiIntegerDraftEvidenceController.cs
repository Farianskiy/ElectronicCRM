using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetMultiIntegerDrafts;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionMultiIntegerDraftEvidenceController : ControllerBase
{
    [HttpGet("{draftId:guid}/evidence")]
    public async Task<ActionResult<CatalogRecognitionMultiIntegerDraftEvidencePage>> GetEvidence(
        [FromRoute] Guid draftId,
        [FromServices] ICatalogRecognitionMultiIntegerDraftReader reader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (draftId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите идентификатор черновика."
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

        var result = await reader.GetEvidenceAsync(
            draftId,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Detail = "Составной черновик не найден."
            });
        }

        return Ok(result);
    }
}