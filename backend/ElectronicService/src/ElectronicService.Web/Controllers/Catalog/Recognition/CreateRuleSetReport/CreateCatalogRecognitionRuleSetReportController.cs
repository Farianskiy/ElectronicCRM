using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.CreateRuleSetReport;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-versions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CreateCatalogRecognitionRuleSetReportController
    : ControllerBase
{
    [HttpPost("{versionId:guid}/reports/batches/{batchId:guid}")]
    public async Task<ActionResult<CatalogRecognitionRuleSetReportCreated>> Create(
        [FromRoute] Guid versionId,
        [FromRoute] Guid batchId,
        [FromServices] ICatalogRecognitionRuleSetReportCreator creator,
        CancellationToken cancellationToken)
    {
        var result = await creator.CreateAsync(
            versionId,
            batchId,
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
            "training.conflict" => StatusCodes.Status409Conflict,
            "training.scope_mismatch" => StatusCodes.Status409Conflict,
            "training.invalid_data" => StatusCodes.Status422UnprocessableEntity,
            "training.invalid_report" => StatusCodes.Status422UnprocessableEntity,
            "training.selection_too_large" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось сформировать отчёт проверки.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}