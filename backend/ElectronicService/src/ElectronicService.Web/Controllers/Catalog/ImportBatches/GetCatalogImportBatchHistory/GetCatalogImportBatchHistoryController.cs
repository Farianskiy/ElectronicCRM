using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatchHistory;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportBatchHistory;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportBatchHistoryController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpGet("{batchId:guid}/history")]
    [ProducesResponseType(
        typeof(GetCatalogImportBatchHistoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCatalogImportBatchHistoryResponse>> Get(
        Guid batchId,
        [FromServices] GetCatalogImportBatchHistoryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogImportBatchHistoryQuery(
                batchId,
                currentUserId);

        var result = await handler
            .Handle(
                query,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var items = result.Value.Items
            .Select(item =>
                new CatalogImportBatchHistoryItemResponse(
                    item.EventType,
                    item.OccurredAtUtc,
                    item.ActorUserId,
                    item.ActorDisplayName,
                    item.ActorEmail,
                    item.ActorUserType,
                    item.Comment))
            .ToArray();

        var response =
            new GetCatalogImportBatchHistoryResponse(
                result.Value.BatchId,
                items);

        return Ok(response);
    }
}