using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.RecheckIntegerDraft;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/integer-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecheckCatalogRecognitionIntegerDraftController : ControllerBase
{
    [HttpGet("{draftId:guid}/recheck")]
    public async Task<ActionResult<CatalogRecognitionIntegerDraftRecheckResult>> Get(
        Guid draftId,
        [FromServices] CatalogRecognitionIntegerDraftRecheckService service,
        CancellationToken cancellationToken)
    {
        if (draftId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите идентификатор числового черновика."
            });
        }

        var result = await service.RecheckAsync(draftId, cancellationToken).ConfigureAwait(false);

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
}