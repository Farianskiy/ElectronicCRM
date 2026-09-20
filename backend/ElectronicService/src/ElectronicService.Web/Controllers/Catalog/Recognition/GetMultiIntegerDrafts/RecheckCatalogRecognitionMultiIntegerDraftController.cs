using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetMultiIntegerDrafts;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecheckCatalogRecognitionMultiIntegerDraftController : ControllerBase
{
    [HttpGet("{draftId:guid}/recheck")]
    public async Task<ActionResult<CatalogRecognitionMultiIntegerDraftRecheckResult>> Get(
        [FromRoute] Guid draftId,
        [FromServices] CatalogRecognitionMultiIntegerDraftRecheckService service,
        CancellationToken cancellationToken)
    {
        if (draftId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "Укажите идентификатор черновика."
            });
        }

        var result = await service.RecheckAsync(draftId, cancellationToken).ConfigureAwait(false);

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