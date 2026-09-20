using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewLiteralDraftBatch;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/literal-drafts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionLiteralDraftBatchController : ControllerBase
{
    [HttpGet("{draftId:guid}/batches/{batchId:guid}/preview")]
    public async Task<ActionResult<CatalogRecognitionLiteralBatchPreviewPage>> Get(
        Guid draftId,
        Guid batchId,
        [FromServices] CatalogRecognitionLiteralBatchPreviewService service,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await service.PreviewAsync(draftId, batchId, page, pageSize, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить черновик на импорте.",
            Detail = result.Error.Message
        });
    }
}