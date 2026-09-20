using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewMultiIntegerBatch;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/multi-integer-proposals/batch-preview")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionMultiIntegerBatchController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2097152)]
    public async Task<ActionResult<CatalogRecognitionMultiIntegerBatchPreviewPage>> Post(
        [FromBody] CatalogRecognitionMultiIntegerBatchPreviewRequest request,
        [FromServices] CatalogRecognitionMultiIntegerBatchPreviewService service,
        CancellationToken cancellationToken)
    {
        var result = await service.PreviewAsync(request, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            "training.selection_too_large" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить составное предложение на импорте.",
            Detail = result.Error.Message
        });
    }
}