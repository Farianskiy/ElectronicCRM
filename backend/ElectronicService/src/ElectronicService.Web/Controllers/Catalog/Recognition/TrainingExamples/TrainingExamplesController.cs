using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.TrainingExamples;

[ApiController]
[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[Route("api/catalog/recognition/training-examples")]
public sealed class TrainingExamplesController(ICatalogTrainingExampleManagement management) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TrainingExampleFilter filter, CancellationToken cancellationToken)
    {
        var result = await management.ListAsync(filter, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await management.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Error);
    }

    [HttpPost("{id:guid}/purpose")]
    public async Task<IActionResult> Purpose(Guid id, [FromBody] ExamplePurposeRequest request, CancellationToken cancellationToken)
    {
        var result = await management.SetPurposeAsync(id, request.EvaluationOnly, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : Failure(result.Error);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeExampleRequest request, CancellationToken cancellationToken)
    {
        var result = await management.RevokeAsync(id, request.Reason, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Ok(new { message = "Подтверждение отозвано. Действующие правила остаются активными; их изменение требует отдельной проверки и переключения." }) : Failure(result.Error);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] TrainingExampleFilter filter, CancellationToken cancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"confirmed-examples-{Path.GetRandomFileName()}");
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose, BufferSize = 65536
        });
        var result = await management.ExportAsync(filter, stream, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) return Failure(result.Error);
        stream.Position = 0;
        Response.ContentType = "application/x-ndjson";
        Response.ContentLength = stream.Length;
        Response.Headers.ContentDisposition = "attachment; filename=confirmed-examples-v1.jsonl";
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        await stream.CopyToAsync(Response.Body, cancellationToken).ConfigureAwait(false);
        return new EmptyResult();
    }

    private ObjectResult Failure(DomainError error)
    {
        var status = error.Code switch
        {
            "training.unauthorized" => 401, "training.forbidden" => 403,
            "training.not_found" => 404, _ => 400
        };
        return StatusCode(status, new ProblemDetails { Status = status, Title = "Обучающие примеры", Detail = error.Message });
    }
}

public sealed record RevokeExampleRequest(string Reason);
public sealed record ExamplePurposeRequest([property: System.Text.Json.Serialization.JsonRequired] bool EvaluationOnly);
