using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.SubmitCatalogImportBatch;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.SubmitCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Regular,Manager")]
[Route("api/catalog/import-batches")]
public sealed class SubmitCatalogImportBatchController : ControllerBase
{
    private const string ProblemTitle = "Не удалось отправить пакет импорта на проверку.";

    [HttpPost("{batchId:guid}/submit")]
    [ProducesResponseType(typeof(SubmitCatalogImportBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitCatalogImportBatchResponse>> Submit(
        Guid batchId,
        [FromServices] SubmitCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command = new SubmitCatalogImportBatchCommand(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response = new SubmitCatalogImportBatchResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.SubmittedAtUtc,
            result.Value.Version);

        return Ok(response);
    }
}