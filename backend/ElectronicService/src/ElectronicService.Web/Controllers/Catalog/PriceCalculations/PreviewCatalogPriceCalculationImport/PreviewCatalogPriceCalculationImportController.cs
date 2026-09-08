using ElectronicService.Contracts.Catalog.PriceCalculations;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.GetCatalogPriceCalculation;
using ElectronicService.Core.Catalog.PriceCalculations.Import;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Web.Controllers.Catalog.PriceCalculations.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceCalculations.PreviewCatalogPriceCalculationImport;

[ApiController]
[Authorize]
[Route("api/catalog/price-calculations")]
public sealed class PreviewCatalogPriceCalculationImportController
    : ControllerBase
{
    private const long MaximumFileSizeBytes =
        20 * 1024 * 1024;

    private const long MaximumMultipartRequestSizeBytes =
        MaximumFileSizeBytes + 1_048_576;

    private const string ProblemTitle =
        "Не удалось проверить файл проекта.";

    [HttpPost(
        "{calculationId:guid}/import-preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        MaximumMultipartRequestSizeBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit =
            MaximumMultipartRequestSizeBytes)]
    [ProducesResponseType(
        typeof(
            PreviewCatalogPriceCalculationImportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<
        PreviewCatalogPriceCalculationImportResponse>>
        Preview(
            Guid calculationId,
            [FromForm] IFormFile? file,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            GetCatalogPriceCalculationQueryHandler
                calculationHandler,
            [FromServices]
            ICatalogPriceCalculationWorkbookPreviewer
                previewer,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);

        ArgumentNullException.ThrowIfNull(
            calculationHandler);

        ArgumentNullException.ThrowIfNull(
            previewer);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        if (file is null || file.Length == 0)
        {
            return Problem(
                detail:
                    "Файл проекта не передан.",
                statusCode:
                    StatusCodes.Status400BadRequest,
                title: ProblemTitle);
        }

        if (file.Length > MaximumFileSizeBytes)
        {
            return Problem(
                detail:
                    "Максимальный размер файла — 20 МБ.",
                statusCode:
                    StatusCodes.Status413PayloadTooLarge,
                title: ProblemTitle);
        }

        var calculationResult =
            await calculationHandler
                .Handle(
                    new GetCatalogPriceCalculationQuery(
                        calculationId,
                        currentUserProvider.UserId.Value),
                    cancellationToken)
                .ConfigureAwait(false);

        if (calculationResult.IsFailure)
        {
            return this
                .ToCatalogPriceCalculationProblem(
                    calculationResult.Error,
                    ProblemTitle);
        }

        var calculation =
            calculationResult.Value;

        if (calculation.Status
            != CatalogPriceCalculationStatus.Draft)
        {
            return this
                .ToCatalogPriceCalculationProblem(
                    CatalogPriceCalculationErrors
                        .CannotModifyCalculation(
                            calculation.Status),
                    ProblemTitle);
        }

        try
        {
            await using var stream =
                file.OpenReadStream();

            var preview =
                await previewer
                    .PreviewAsync(
                        stream,
                        file.FileName,
                        cancellationToken)
                    .ConfigureAwait(false);

            var rows =
                preview.Rows
                    .Select(
                        row =>
                            new CatalogPriceCalculationImportPreviewRowResponse(
                                row.RowNumber,
                                row.Article,
                                row.SourceName,
                                row.SourceManufacturer,
                                row.Quantity,
                                row.Status.ToString(),
                                row.Message,
                                row.ProductId,
                                row.ProductArticle,
                                row.ProductName,
                                row.ManufacturerId,
                                row.ManufacturerName,
                                row.StockQuantity,
                                row.ShortageQuantity,
                                row.PriceListId,
                                row.PriceListRowId,
                                row.BasePriceAmount,
                                row.MrcPriceAmount))
                    .ToArray();

            return Ok(
                new PreviewCatalogPriceCalculationImportResponse(
                    preview.ReadRowsCount,
                    preview.MatchedRowsCount,
                    preview.SkippedRowsCount,
                    rows));
        }
        catch (InvalidDataException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status400BadRequest,
                title: ProblemTitle);
        }
    }
}