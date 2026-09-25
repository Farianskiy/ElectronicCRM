using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/assistant/dictionary-suggestions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DictionaryEvaluationController(IDictionaryEvaluationReports reports) : ControllerBase
{
    [HttpPost("{id:guid}/evaluations")]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> Create(Guid id, [FromBody] ApproveCatalogAssistantDictionarySuggestionRequest request, CancellationToken ct)
    {
        var result = await reports.CreateAsync(new ApproveCatalogAssistantDictionarySuggestionCommand(id, request.Phrase, request.Kind, request.TargetCode,
            request.TargetValue, request.ProductTypeCode, request.Priority, request.ReviewComment, request.EvidenceRevision), ct).ConfigureAwait(false);
        return result.IsSuccess ? Ok(new { reportId = result.Value }) : DictionaryEvaluationHttp.Problem(this, result.Error);
    }
    [HttpGet("evaluations/{id:guid}")]
    public Task<IActionResult> Read(Guid id, CancellationToken ct, [FromQuery] int page = 1) => RespondAsync(id, page, false, ct);
    [HttpPost("evaluations/{id:guid}/replay")]
    public Task<IActionResult> Replay(Guid id, CancellationToken ct, [FromQuery] int page = 1) => RespondAsync(id, page, true, ct);
    private async Task<IActionResult> RespondAsync(Guid id, int page, bool replay, CancellationToken ct)
    {
        var result = await reports.ReadAsync(id, page, replay, ct).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : DictionaryEvaluationHttp.Problem(this, result.Error);
    }
}

internal static class DictionaryEvaluationHttp
{
    public static IActionResult Problem(ControllerBase controller, DomainError error)
    {
        var status = error.Code switch
        {
            "training.unauthorized" => 401, "training.forbidden" => 403, "training.not_found" => 404,
            "evaluation.stale" or "evaluation.not_ready" or "evaluation.report_required" or "evaluation.unsupported_format" or "training.conflict" => 409,
            "evaluation.timeout" or "evaluation.selection_too_large" or "evaluation.invalid_snapshot" => 422, _ => 400
        };
        var problem = new ProblemDetails { Status = status, Title = "Оценка словарного предложения", Detail = error.Message };
        problem.Extensions["code"] = error.Code;
        return controller.StatusCode(status, problem);
    }
}
