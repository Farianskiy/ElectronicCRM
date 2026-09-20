using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetRuleSetReport;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-reports")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GetCatalogRecognitionRuleSetReportController
    : ControllerBase
{
    [HttpGet("{reportId:guid}")]
    public async Task<ActionResult<CatalogRecognitionRuleSetReportPage>> GetPage(
        [FromRoute] Guid reportId,
        [FromServices] ICatalogRecognitionRuleSetReportReader reader,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await reader.GetPageAsync(
            reportId,
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
            "training.invalid_report" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось прочитать отчёт проверки.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<
    IReadOnlyList<CatalogRecognitionRuleSetReportCreated>>> GetRecent(
        [FromQuery] Guid versionId,
        [FromQuery] Guid batchId,
        [FromServices] ICatalogRecognitionRuleSetReportReader reader,
        CancellationToken cancellationToken)
    {
        var result = await reader.GetRecentAsync(
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
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Не удалось загрузить список отчётов.",
            Detail = result.Error.Message
        };

        problem.Extensions["code"] = result.Error.Code;

        return StatusCode(statusCode, problem);
    }
}