using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationReader
    : ICatalogPriceCalculationReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceCalculationReader(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogPriceCalculationAccess?>
    GetAccessAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogPriceCalculations
            .AsNoTracking()
            .Where(
                calculation =>
                    calculation.Id == calculationId)
            .Select(
                calculation =>
                    new CatalogPriceCalculationAccess(
                        calculation.Id,
                        calculation.CreatedByUserId,
                        calculation.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogPriceCalculationDetails?>
        GetByIdAsync(
            Guid calculationId,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
    {
        var header =
            await _dbContext.CatalogPriceCalculations
                .AsNoTracking()
                .Where(
                    calculation =>
                        calculation.Id == calculationId
                        && calculation.CreatedByUserId
                            == createdByUserId)
                .Select(
                    calculation =>
                        new StoredCalculationHeader(
                            calculation.Id,
                            calculation.CreatedByUserId,
                            calculation.Title,
                            calculation.Currency,
                            calculation.CustomerName,
                            calculation.ObjectName,
                            calculation.ProjectNumber,
                            calculation.ResponsibleName,
                            calculation.Comment,
                            calculation.ValidUntil,
                            calculation.Status,
                            calculation.TotalAmount,
                            calculation.CreatedAtUtc,
                            calculation.UpdatedAtUtc,
                            calculation.CompletedAtUtc,
                            calculation.ArchivedAtUtc))
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        if (header is null)
        {
            return null;
        }

        var lines =
            await (
                from line in
                    _dbContext.CatalogPriceCalculationLines
                        .AsNoTracking()
                join product in
                    _dbContext.Products.AsNoTracking()
                    on line.ProductId equals product.Id
                where line.CalculationId == calculationId
                orderby line.CreatedAtUtc, line.Id
                select new CatalogPriceCalculationLineDetails(
                    line.Id,
                    line.ProductId,
                    line.ManufacturerId,
                    line.ManufacturerName,
                    line.PriceListId,
                    line.PriceListRowId,
                    line.Article,
                    line.Name,
                    line.Unit,
                    line.Quantity,
                    product.StockQuantity.Value,
                    line.Quantity > product.StockQuantity.Value
                        ? line.Quantity - product.StockQuantity.Value
                        : 0m,
                    line.BasePriceAmount,
                    line.MrcPriceAmount,
                    line.DiscountPercent,
                    line.ProjectPriceAmount,
                    line.TotalAmount,
                    line.CreatedAtUtc,
                    line.UpdatedAtUtc))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var discounts =
            await (
                from discount in
                    _dbContext
                        .CatalogPriceCalculationManufacturerDiscounts
                        .AsNoTracking()
                join manufacturer in
                    _dbContext.Manufacturers.AsNoTracking()
                    on discount.ManufacturerId
                    equals manufacturer.Id
                where discount.CalculationId
                      == calculationId
                orderby manufacturer.Name
                select new CatalogPriceCalculationDiscountDetails(
                    discount.Id,
                    discount.ManufacturerId,
                    manufacturer.Name,
                    discount.DiscountPercent,
                    discount.CreatedAtUtc,
                    discount.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CatalogPriceCalculationDetails(
            header.CalculationId,
            header.CreatedByUserId,
            header.Title,
            header.Currency,
            header.CustomerName,
            header.ObjectName,
            header.ProjectNumber,
            header.ResponsibleName,
            header.Comment,
            header.ValidUntil,
            header.Status,
            header.TotalAmount,
            header.CreatedAtUtc,
            header.UpdatedAtUtc,
            header.CompletedAtUtc,
            header.ArchivedAtUtc,
            lines,
            discounts);
    }

    public async Task<CatalogPriceCalculationsPage>
        GetOwnAsync(
            Guid createdByUserId,
            CatalogPriceCalculationStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.CatalogPriceCalculations
                .AsNoTracking()
                .Where(
                    calculation =>
                        calculation.CreatedByUserId
                        == createdByUserId);

        if (status.HasValue)
        {
            query =
                query.Where(
                    calculation =>
                        calculation.Status
                        == status.Value);
        }

        var totalCount =
            await query
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var items =
            await query
                .OrderByDescending(
                    calculation =>
                        calculation.UpdatedAtUtc
                        ?? calculation.CreatedAtUtc)
                .ThenByDescending(
                    calculation =>
                        calculation.CreatedAtUtc)
                .Select(
                    calculation =>
                        new CatalogPriceCalculationListItem(
                            calculation.Id,
                            calculation.Title,
                            calculation.Currency,
                            calculation.Status,
                            calculation.TotalAmount,
                            _dbContext
                                .CatalogPriceCalculationLines
                                .Count(
                                    line =>
                                        line.CalculationId
                                        == calculation.Id),
                            _dbContext
                                .CatalogPriceCalculationLines
                                .Where(
                                    line =>
                                        line.CalculationId
                                        == calculation.Id)
                                .Select(
                                    line =>
                                        line.ManufacturerId)
                                .Distinct()
                                .Count(),
                            calculation.CreatedAtUtc,
                            calculation.UpdatedAtUtc,
                            calculation.CompletedAtUtc,
                            calculation.ArchivedAtUtc))
                .Skip(skip)
                .Take(take)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        return new CatalogPriceCalculationsPage(
            totalCount,
            items);
    }

    private sealed record StoredCalculationHeader(
        Guid CalculationId,
        Guid CreatedByUserId,
        string Title,
        string Currency,
        string? CustomerName,
        string? ObjectName,
        string? ProjectNumber,
        string? ResponsibleName,
        string? Comment,
        DateOnly? ValidUntil,
        CatalogPriceCalculationStatus Status,
        decimal TotalAmount,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        DateTime? CompletedAtUtc,
        DateTime? ArchivedAtUtc);
}