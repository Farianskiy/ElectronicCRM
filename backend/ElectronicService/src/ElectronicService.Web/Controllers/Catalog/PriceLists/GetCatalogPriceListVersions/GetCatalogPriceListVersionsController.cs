using ElectronicService.Contracts.Catalog.PriceLists;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.GetCatalogPriceListVersions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Web.Controllers.Catalog.PriceLists.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.PriceLists.GetCatalogPriceListVersions;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/price-lists")]
public sealed class GetCatalogPriceListVersionsController
    : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось получить версии прайс-листа.";

    [HttpGet]
    [ProducesResponseType(
        typeof(GetCatalogPriceListVersionsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<GetCatalogPriceListVersionsResponse>>
        Get(
            [FromQuery]
            Guid manufacturerId,
            [FromQuery]
            CatalogPriceListStatus? status,
            [FromQuery]
            int page,
            [FromQuery]
            int pageSize,
            [FromServices]
            ICurrentUserProvider currentUserProvider,
            [FromServices]
            GetCatalogPriceListVersionsQueryHandler handler,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            currentUserProvider);

        ArgumentNullException.ThrowIfNull(handler);

        if (!currentUserProvider.UserId.HasValue)
        {
            return this.ToCurrentUserProblem();
        }

        var actualPage =
            page <= 0
                ? 1
                : page;

        var actualPageSize =
            pageSize <= 0
                ? 20
                : pageSize;

        var query =
            new GetCatalogPriceListVersionsQuery(
                manufacturerId,
                currentUserProvider.UserId.Value,
                status,
                actualPage,
                actualPageSize);

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

        var totalPages =
            result.Value.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    result.Value.TotalCount
                    / (double)actualPageSize);

        var items =
            result.Value.Items
                .Select(
                    item =>
                        new CatalogPriceListVersionResponse(
                            item.PriceListId,
                            item.OriginalFileName,
                            item.FileSizeBytes,
                            item.EffectiveDate,
                            item.Status.ToString(),
                            item.RowsCount,
                            item.ValidRowsCount,
                            item.ErrorRowsCount,
                            item.CreatedAtUtc,
                            item.ProcessedAtUtc,
                            item.ActivatedAtUtc,
                            item.ArchivedAtUtc,
                            item.FailureReason))
                .ToArray();

        return Ok(
            new GetCatalogPriceListVersionsResponse(
                manufacturerId,
                actualPage,
                actualPageSize,
                result.Value.TotalCount,
                totalPages,
                items));
    }
}