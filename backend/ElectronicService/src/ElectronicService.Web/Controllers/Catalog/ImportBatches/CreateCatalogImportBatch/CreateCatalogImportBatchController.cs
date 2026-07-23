using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.CreateCatalogImportBatch;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.CreateCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class
    CreateCatalogImportBatchController
    : ControllerBase
{
    /*
     * Сам Excel ограничен 10 МБ.
     *
     * Multipart-запрос немного больше файла,
     * потому что содержит boundary и headers.
     */
    private const long
        MaximumMultipartRequestSizeBytes =
            CatalogImportBatch
                .MaximumFileSizeBytes
            + 1_048_576;

    private const string
        DefaultExcelContentType =
            "application/vnd.openxmlformats-" +
            "officedocument.spreadsheetml.sheet";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        MaximumMultipartRequestSizeBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit =
            MaximumMultipartRequestSizeBytes)]
    [ProducesResponseType(
        typeof(CreateCatalogImportBatchResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(
        StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<
        CreateCatalogImportBatchResponse>> Create(
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

        if (!User.TryGetUserId(
                out var currentUserId))
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Пользователь не определён.",
                detail:
                    "В JWT отсутствует корректный " +
                    "идентификатор пользователя.");
        }

        /*
         * Быстрая проверка до открытия потока.
         * Core всё равно повторно проверит
         * фактически прочитанный размер.
         */
        if (file.Length == 0)
        {
            return ToProblem(
                CatalogImportErrors.FileIsEmpty());
        }

        if (file.Length
            > CatalogImportBatch
                .MaximumFileSizeBytes)
        {
            return ToProblem(
                CatalogImportErrors
                    .FileIsTooLarge(
                        CatalogImportBatch
                            .MaximumFileSizeBytes));
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
            return ToProblem(result.Error);
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

    private ObjectResult ToProblem(
        DomainError error)
    {
        var statusCode =
            error.Code switch
            {
                "catalog.import.file.too_large"
                    => StatusCodes
                        .Status413PayloadTooLarge,

                "catalog.import.file.unsupported_extension"
                    => StatusCodes
                        .Status415UnsupportedMediaType,

                "catalog.import.current_user.not_found"
                    => StatusCodes
                        .Status401Unauthorized,

                "catalog.import.user.cannot_create"
                    => StatusCodes
                        .Status403Forbidden,

                _ => StatusCodes
                    .Status400BadRequest
            };

        var problemDetails =
            new ProblemDetails
            {
                Status = statusCode,
                Title =
                    "Не удалось создать " +
                    "пакет импорта.",
                Detail = error.Message,
                Type = error.Code
            };

        return StatusCode(
            statusCode,
            problemDetails);
    }
}