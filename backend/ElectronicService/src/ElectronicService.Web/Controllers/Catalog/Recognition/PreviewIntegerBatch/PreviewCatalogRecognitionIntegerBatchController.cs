using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewIntegerBatch;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/integer-pattern-proposals/batch-preview")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionIntegerBatchController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(262144)]
    public async Task<ActionResult<CatalogRecognitionIntegerBatchPreviewPage>> Post(
        [FromBody] CatalogRecognitionIntegerBatchPreviewRequest request,
        [FromServices] CatalogRecognitionIntegerBatchPreviewService service,
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
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить структурное предложение.",
            Detail = result.Error.Message
        });
    }
}