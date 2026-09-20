using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewRuleSetBatch;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionRuleSetBatchController : ControllerBase
{
    [HttpGet("{versionId:guid}/preview-batch/{batchId:guid}")]
    public async Task<ActionResult<CatalogRecognitionRuleSetBatchPreviewPage>> Preview(
        [FromRoute] Guid versionId,
        [FromRoute] Guid batchId,
        [FromServices] CatalogRecognitionRuleSetBatchPreviewService service,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await service.PreviewAsync(
            versionId,
            batchId,
            page,
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.unauthorized" => StatusCodes.Status401Unauthorized,
            "training.forbidden" => StatusCodes.Status403Forbidden,
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.scope_mismatch" => StatusCodes.Status409Conflict,
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить импорт выбранной версией.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}