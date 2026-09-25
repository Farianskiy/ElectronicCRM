using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.SwitchRuleSet;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-switches")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SwitchCatalogRecognitionRuleSetController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(16384)]
    public async Task<ActionResult<CatalogRecognitionRuleSetSwitchResult>> Switch(
        [FromBody] CatalogRecognitionRuleSetSwitchCommand command,
        [FromServices] ICatalogRecognitionRuleSetSwitcher switcher,
        CancellationToken cancellationToken)
    {
        var result = await switcher.SwitchAsync(
            command,
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
            "training.conflict" or "evaluation.stale" or "evaluation.not_ready" or "evaluation.legacy_report" or "evaluation.unsupported_format" => StatusCodes.Status409Conflict,
            "evaluation.invalid_snapshot" => StatusCodes.Status422UnprocessableEntity,
            "training.scope_mismatch" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            "training.invalid_report" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось переключить версию правил.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}