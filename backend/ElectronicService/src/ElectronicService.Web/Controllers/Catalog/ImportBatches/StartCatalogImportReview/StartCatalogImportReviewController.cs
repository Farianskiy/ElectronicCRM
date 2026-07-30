using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.StartCatalogImportReview;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.StartCatalogImportReview;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class StartCatalogImportReviewController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpPost("{batchId:guid}/review/start")]
    [ProducesResponseType(typeof(StartCatalogImportReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartCatalogImportReviewResponse>> Start(
        Guid batchId,
        [FromServices] StartCatalogImportReviewCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command = new StartCatalogImportReviewCommand(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response = new StartCatalogImportReviewResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.ReviewedByUserId,
            result.Value.ReviewedAtUtc,
            result.Value.Version);

        return Ok(response);
    }
}