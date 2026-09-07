using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.UploadCatalogPriceList;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.UploadCatalogPriceList;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class UploadCatalogPriceListController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось загрузить прайс-лист.";

    private const long MaximumMultipartRequestSizeBytes =
        CatalogPriceList.MaximumFileSizeBytes
        + 1_048_576;

    private const string DefaultContentType =
        "application/octet-stream";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        MaximumMultipartRequestSizeBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit =
            MaximumMultipartRequestSizeBytes)]
    [ProducesResponseType(
        typeof(UploadCatalogPriceListResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(
        StatusCodes.Status415UnsupportedMediaType)]
    public async Task<
        ActionResult<UploadCatalogPriceListResponse>>
        Upload(
            [FromForm]
            Guid manufacturerId,
            [FromForm]
            IFormFile? file,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            UploadCatalogPriceListCommandHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);

        ArgumentNullException.ThrowIfNull(
            handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        if (manufacturerId == Guid.Empty)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Производитель не выбран.",
                detail:
                    "Передайте идентификатор производителя в поле 'manufacturerId'.");
        }

        if (file is null)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Файл прайс-листа не передан.",
                detail:
                    "Добавьте ZIP- или XLSX-файл в поле 'file'.");
        }

        if (file.Length == 0)
        {
            return this.ToCatalogPriceListProblem(
                CatalogPriceListErrors.FileIsEmpty(),
                ProblemTitle);
        }

        if (file.Length
            > CatalogPriceList.MaximumFileSizeBytes)
        {
            return this.ToCatalogPriceListProblem(
                CatalogPriceListErrors.FileIsTooLarge(
                    CatalogPriceList.MaximumFileSizeBytes),
                ProblemTitle);
        }

        var contentType =
            string.IsNullOrWhiteSpace(
                file.ContentType)
                ? DefaultContentType
                : file.ContentType;

        await using var fileStream =
            file.OpenReadStream();

        var command =
            new UploadCatalogPriceListCommand(
                manufacturerId,
                currentUserProvider.UserId.Value,
                fileStream,
                file.FileName,
                contentType);

        var result =
            await handler
                .Handle(
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(
                result.Error,
                ProblemTitle);
        }

        var response =
            new UploadCatalogPriceListResponse(
                result.Value.PriceListId,
                result.Value.ManufacturerId,
                result.Value.OriginalFileName,
                result.Value.FileSizeBytes,
                result.Value.Status.ToString());

        return Created(
            new Uri(
                $"/api/catalog/price-lists/"
                + $"{result.Value.PriceListId}",
                UriKind.Relative),
            response);
    }
}