using ElectronicService.Contracts.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.PreviewRuleSetName;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreviewCatalogRecognitionRuleSetNameController : ControllerBase
{
    [HttpPost("{versionId:guid}/preview-name")]
    [RequestSizeLimit(32768)]
    public async Task<ActionResult<CatalogRecognitionRuleSetNamePreviewResult>> Preview(
        [FromRoute] Guid versionId,
        [FromBody] PreviewCatalogRecognitionRuleSetNameRequest request,
        [FromServices] CatalogRecognitionRuleSetNamePreviewService service,
        CancellationToken cancellationToken)
    {
        var result = await service.PreviewAsync(
            versionId,
            request.ManufacturerId,
            request.ProductTypeId,
            request.ProductName,
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.scope_mismatch" => StatusCodes.Status409Conflict,
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить название выбранной версией правил.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}