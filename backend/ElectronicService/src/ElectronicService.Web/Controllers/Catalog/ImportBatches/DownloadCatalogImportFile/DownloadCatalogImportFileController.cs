using ElectronicService.Core.Catalog.ImportBatches.DownloadCatalogImportFile;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.DownloadCatalogImportFile;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class DownloadCatalogImportFileController : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("{batchId:guid}/file")]
    [Produces(ExcelContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download(
        Guid batchId,
        [FromServices] DownloadCatalogImportFileQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Пользователь не определён.",
                detail: "В JWT отсутствует корректный идентификатор пользователя.");
        }

        var query = new DownloadCatalogImportFileQuery(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(
            result.Value.Content.ToArray(),
            result.Value.ContentType,
            result.Value.FileName);
    }

    private ObjectResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            "catalog.import.current_user.not_found"
                => StatusCodes.Status401Unauthorized,

            "catalog.import.batch.access_denied"
                => StatusCodes.Status403Forbidden,

            "catalog.import.batch.not_found"
                => StatusCodes.Status404NotFound,

            "catalog.import.file.cannot_be_read"
                => StatusCodes.Status500InternalServerError,

            "catalog.import.file.integrity_check_failed"
                => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            statusCode,
            new ProblemDetails
            {
                Status = statusCode,
                Title = "Не удалось скачать исходный Excel-файл.",
                Detail = error.Message,
                Type = error.Code
            });
    }
}