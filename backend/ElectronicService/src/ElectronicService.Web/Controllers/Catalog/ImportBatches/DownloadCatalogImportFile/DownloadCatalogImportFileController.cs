using ElectronicService.Core.Catalog.ImportBatches.DownloadCatalogImportFile;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.DownloadCatalogImportFile;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class DownloadCatalogImportFileController : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
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
            return this.ToCurrentUserProblem();
        }

        var query = new DownloadCatalogImportFileQuery(
            batchId,
            currentUserId);

        var result = await handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(
            result.Value.Content.ToArray(),
            result.Value.ContentType,
            result.Value.FileName);
    }
}