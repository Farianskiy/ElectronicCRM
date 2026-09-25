using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.TrainingExamples;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/learning-provenance")]
public sealed class LearningProvenanceController(ILearningProvenance provenance) : ControllerBase
{
    [HttpGet("examples/{id:guid}")]
    public async Task<IActionResult> Example(Guid id, CancellationToken cancellationToken, [FromQuery] int page = 1) =>
        Respond(await provenance.ReadAsync(id, true, page, cancellationToken).ConfigureAwait(false));

    [HttpGet("feedback/{id:guid}")]
    [HttpGet("feedback/{id:guid}/preview-impact")]
    public async Task<IActionResult> Feedback(Guid id, CancellationToken cancellationToken, [FromQuery] int page = 1) =>
        Respond(await provenance.ReadAsync(id, false, page, cancellationToken).ConfigureAwait(false));

    [HttpPost("feedback/{id:guid}/exclude")]
    public async Task<IActionResult> Exclude(Guid id, [FromBody] ExcludeObservationRequest request, CancellationToken cancellationToken) =>
        Respond(await provenance.ExcludeAsync(id, request.Reason, cancellationToken).ConfigureAwait(false));

    [HttpGet("suggestions/{id:guid}")]
    public async Task<IActionResult> Suggestion(Guid id, CancellationToken cancellationToken) =>
        Respond(await provenance.ReadSuggestionAsync(id, cancellationToken).ConfigureAwait(false));

    private IActionResult Respond<T>(Result<T, DomainError> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var status = result.Error.Code switch
        {
            "training.unauthorized" => 401, "training.forbidden" => 403,
            "training.not_found" => 404, "training.conflict" => 409, _ => 400
        };
        return StatusCode(status, new ProblemDetails { Status = status, Detail = result.Error.Message });
    }
}

public sealed record ExcludeObservationRequest(string Reason);
