using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.RejectCatalogImportBatch;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.RejectCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class RejectCatalogImportBatchController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpPost("{batchId:guid}/review/reject")]
    [ProducesResponseType(typeof(RejectCatalogImportBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RejectCatalogImportBatchResponse>> Reject(
        Guid batchId,
        [FromBody] RejectCatalogImportBatchRequest request,
        [FromServices] RejectCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command = new RejectCatalogImportBatchCommand(
            batchId,
            currentUserId,
            request.Reason);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response = new RejectCatalogImportBatchResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.RejectedByUserId,
            result.Value.RejectedAtUtc,
            result.Value.RejectionReason,
            result.Value.Version);

        return Ok(response);
    }
}