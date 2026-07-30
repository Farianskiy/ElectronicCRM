using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.RequestCatalogImportChanges;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.RequestCatalogImportChanges;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/import-batches")]
public sealed class RequestCatalogImportChangesController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpPost("{batchId:guid}/review/request-changes")]
    [ProducesResponseType(typeof(RequestCatalogImportChangesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequestCatalogImportChangesResponse>> RequestChanges(
        Guid batchId,
        [FromBody] RequestCatalogImportChangesRequest request,
        [FromServices] RequestCatalogImportChangesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command = new RequestCatalogImportChangesCommand(
            batchId,
            currentUserId,
            request.Comment);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response = new RequestCatalogImportChangesResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.ChangesRequestedByUserId,
            result.Value.ChangesRequestedAtUtc,
            result.Value.Comment,
            result.Value.Version);

        return Ok(response);
    }
}