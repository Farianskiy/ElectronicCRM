using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportMapping;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.UpdateCatalogImportMapping;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class UpdateCatalogImportMappingController : ControllerBase
{
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
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
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
                    return ToProblem(
                        CatalogImportErrors.InvalidColumnTarget(
                            targetKindText));
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
            return ToProblem(result.Error);
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

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.user.cannot_edit"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.product_type.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.mapping.cannot_be_edited"
                => StatusCodes.Status409Conflict,

            "catalog.import.column.duplicate_mapping"
                => StatusCodes.Status409Conflict,

            "catalog.import.batch.concurrency_conflict"
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось сохранить сопоставление колонок.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}