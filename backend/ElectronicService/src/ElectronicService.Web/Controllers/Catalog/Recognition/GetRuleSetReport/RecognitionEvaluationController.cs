using ElectronicService.Core.Catalog.Recognition.Evaluation;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.GetRuleSetReport;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training/rule-set-reports")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecognitionEvaluationController(IRecognitionEvaluationReports reports) : ControllerBase
{
    [HttpGet("{reportId:guid}/evaluation")]
    public Task<IActionResult> Read(Guid reportId, CancellationToken cancellationToken, [FromQuery] int page = 1) =>
        RespondAsync(reportId, page, false, cancellationToken);

    [HttpPost("{reportId:guid}/evaluation/replay")]
    public Task<IActionResult> Replay(Guid reportId, CancellationToken cancellationToken, [FromQuery] int page = 1) =>
        RespondAsync(reportId, page, true, cancellationToken);

    private async Task<IActionResult> RespondAsync(Guid reportId, int page, bool replay, CancellationToken ct)
    {
        var result = await reports.ReadAsync(reportId, page, replay, ct).ConfigureAwait(false);
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "training.unauthorized" => 401, "training.forbidden" => 403, "training.not_found" => 404,
            "evaluation.legacy_report" or "evaluation.unsupported_format" => 409,
            "evaluation.invalid_snapshot" or "evaluation.timeout" => 422, _ => 400
        };
        var problem = new ProblemDetails { Status = status, Title = "Сравнительная оценка", Detail = result.Error.Message };
        problem.Extensions["code"] = result.Error.Code;
        return StatusCode(status, problem);
    }
}
