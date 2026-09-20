using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.CheckRuleSetTraining;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckCatalogRecognitionRuleSetTrainingController
    : ControllerBase
{
    [HttpGet("{versionId:guid}/training-check")]
    public async Task<ActionResult<CatalogRecognitionRuleSetTrainingCheckResult>> Check(
        [FromRoute] Guid versionId,
        [FromServices] CatalogRecognitionRuleSetTrainingCheckService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CheckAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Code switch
        {
            "training.not_found" => StatusCodes.Status404NotFound,
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.scope_mismatch" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось проверить учебные основания версии.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}