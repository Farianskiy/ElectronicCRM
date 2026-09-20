using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.RecheckLiteralDraft;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/literal-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecheckCatalogRecognitionLiteralDraftController : ControllerBase
{
    [HttpGet("{draftId:guid}/recheck")]
    public async Task<ActionResult<CatalogRecognitionLiteralDraftRecheckResult>> Recheck(
        Guid draftId,
        [FromServices] CatalogRecognitionLiteralDraftRecheckService service,
        CancellationToken cancellationToken)
    {
        if (draftId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = "Укажите идентификатор черновика." });
        }

        var result = await service.RecheckAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = "Черновик предложения не найден." });
        }

        return Ok(result);
    }
}