using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.CreateCatalogImportBatch;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.CreateCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class CreateCatalogImportBatchController : ControllerBase
{
    private const string ProblemTitle = "Не удалось создать пакет импорта.";
    private const long MaximumMultipartRequestSizeBytes = CatalogImportBatch.MaximumFileSizeBytes + 1_048_576;
    private const string DefaultExcelContentType = "application/vnd.openxmlformats-" + "officedocument.spreadsheetml.sheet";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumMultipartRequestSizeBytes)]
    [ProducesResponseType(typeof(CreateCatalogImportBatchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<CreateCatalogImportBatchResponse>> Create(
            [FromForm] IFormFile? file,
            [FromServices]
            CreateCatalogImportBatchCommandHandler
            handler,
            CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Excel-файл не передан.",
                detail:
                    "Добавьте файл в поле 'file'.");
        }

        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        if (file.Length == 0)
        {
            return this.ToCatalogImportProblem(
                CatalogImportErrors.FileIsEmpty(),
                ProblemTitle);
        }

        if (file.Length > CatalogImportBatch.MaximumFileSizeBytes)
        {
            return this.ToCatalogImportProblem(
                CatalogImportErrors.FileIsTooLarge(CatalogImportBatch.MaximumFileSizeBytes),
                ProblemTitle);
        }

        var contentType =
            string.IsNullOrWhiteSpace(
                file.ContentType)
                ? DefaultExcelContentType
                : file.ContentType;

        await using var fileStream =
            file.OpenReadStream();

        var command =
            new CreateCatalogImportBatchCommand(
                currentUserId,
                fileStream,
                file.FileName,
                contentType);

        var result = await handler
            .Handle(
                command,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var response =
            new CreateCatalogImportBatchResponse(
                result.Value.BatchId,
                result.Value.Status.ToString());

        return Created(
            new Uri(
                $"/api/catalog/import-batches/" +
                $"{result.Value.BatchId}",
                UriKind.Relative),
            response);
    }
}