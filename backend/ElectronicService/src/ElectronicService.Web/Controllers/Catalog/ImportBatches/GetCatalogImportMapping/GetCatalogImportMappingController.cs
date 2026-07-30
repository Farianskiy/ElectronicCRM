using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportMapping;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportMapping;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportMappingController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpGet("{batchId:guid}/mapping")]
    [ProducesResponseType(typeof(GetCatalogImportMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCatalogImportMappingResponse>> Get(
        Guid batchId,
        [FromServices] GetCatalogImportMappingQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query = new GetCatalogImportMappingQuery(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var columns = result.Value.Columns
            .Select(column => new CatalogImportColumnMappingResponse(
                column.ColumnId,
                column.SourceColumnNumber,
                column.SourceHeader,
                column.TargetKind.ToString(),
                column.CharacteristicDefinitionId,
                column.Confidence,
                column.IsConfirmed))
            .ToArray();

        var response = new GetCatalogImportMappingResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.ProductTypeId,
            columns,
            result.Value.Version,
            result.Value.CanEdit);

        return Ok(response);
    }
}