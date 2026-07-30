using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportMapping;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.UpdateCatalogImportMapping;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class UpdateCatalogImportMappingController : ControllerBase
{
    private const string ProblemTitle = "Не удалось сохранить сопоставление колонок.";
    [HttpPut("{batchId:guid}/mapping")]
    [ProducesResponseType(typeof(UpdateCatalogImportMappingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateCatalogImportMappingResponse>> Update(
        Guid batchId,
        [FromBody] UpdateCatalogImportMappingRequest request,
        [FromServices] UpdateCatalogImportMappingCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var mappings = new List<UpdateCatalogImportColumnMapping>();

        if (request.Columns is not null)
        {
            foreach (var column in request.Columns)
            {
                var targetKindText = column.TargetKind?.Trim() ?? string.Empty;

                if (!Enum.TryParse<CatalogImportColumnTargetKind>(
                        targetKindText,
                        ignoreCase: true,
                        out var targetKind)
                    || !Enum.IsDefined<CatalogImportColumnTargetKind>(targetKind)
                    || targetKind == CatalogImportColumnTargetKind.None)
                {
                    return this.ToCatalogImportProblem(
                        CatalogImportErrors.InvalidColumnTarget(targetKindText),
                        ProblemTitle);
                }

                mappings.Add(
                    new UpdateCatalogImportColumnMapping(
                        column.ColumnId,
                        targetKind,
                        column.CharacteristicDefinitionId));
            }
        }

        var command = new UpdateCatalogImportMappingCommand(
            batchId,
            currentUserId,
            request.ProductTypeId,
            mappings);

        var result = await handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response = new UpdateCatalogImportMappingResponse(
            result.Value.BatchId,
            result.Value.Status.ToString(),
            result.Value.ProductTypeId,
            result.Value.ColumnsCount,
            result.Value.UnmappedColumnsCount,
            result.Value.UnconfirmedColumnsCount,
            result.Value.Version);

        return Ok(response);
    }
}