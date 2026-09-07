using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceList;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.GetCatalogPriceList;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class GetCatalogPriceListController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить прайс-лист.";

    [HttpGet("{priceListId:guid}")]
    [ProducesResponseType(
        typeof(GetCatalogPriceListResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<GetCatalogPriceListResponse>>
        Get(
            Guid priceListId,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            GetCatalogPriceListQueryHandler handler,
            CancellationToken cancellationToken)
    {
        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogPriceListQuery(
                priceListId,
                currentUserProvider.UserId.Value);

        var result =
            await handler
                .Handle(
                    query,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogPriceListProblem(
                result.Error,
                ProblemTitle);
        }

        var value =
            result.Value;

        return Ok(
            new GetCatalogPriceListResponse(
                value.PriceListId,
                value.ManufacturerId,
                value.ManufacturerName,
                value.CreatedByUserId,
                value.OriginalFileName,
                value.ContentType,
                value.FileSizeBytes,
                value.Currency,
                value.VatRatePercent,
                value.EffectiveDate,
                value.Status.ToString(),
                value.RowsCount,
                value.ValidRowsCount,
                value.ErrorRowsCount,
                value.EstimatedRowsCount,
                value.ReadRowsCount,
                value.SavedRowsCount,
                value.CreatedAtUtc,
                value.UpdatedAtUtc,
                value.ProcessedAtUtc,
                value.ActivatedAtUtc,
                value.ArchivedAtUtc,
                value.FailureReason));
    }
}