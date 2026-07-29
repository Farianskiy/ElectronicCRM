using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportMapping;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportMapping;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportMappingController : ControllerBase
{
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
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var query = new GetCatalogImportMappingQuery(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
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

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.user.cannot_view_own"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось получить сопоставление колонок.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}