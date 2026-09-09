using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Core.Catalog.PriceCalculations.Abstractions;

public interface ICatalogPriceCalculationReader
{
    Task<CatalogPriceCalculationAccess?> GetAccessAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceCalculationDetails?> GetByIdAsync(
        Guid calculationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceCalculationsPage> GetOwnAsync(
        Guid createdByUserId,
        CatalogPriceCalculationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogPriceCalculationAccess(
    Guid CalculationId,
    Guid CreatedByUserId,
    CatalogPriceCalculationStatus Status);

public sealed record CatalogPriceCalculationDetails(
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
    DateTime? ArchivedAtUtc,
    IReadOnlyList<CatalogPriceCalculationLineDetails> Lines,
    IReadOnlyList<CatalogPriceCalculationDiscountDetails> ManufacturerDiscounts);

public sealed record CatalogPriceCalculationLineDetails(
    Guid LineId,
    Guid ProductId,
    Guid ManufacturerId,
    string ManufacturerName,
    Guid PriceListId,
    Guid PriceListRowId,
    string Article,
    string Name,
    string? Unit,
    decimal Quantity,
    decimal StockQuantity,
    decimal ShortageQuantity,
    decimal BasePriceAmount,
    decimal? MrcPriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CatalogPriceCalculationDiscountDetails(
    Guid DiscountId,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal DiscountPercent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CatalogPriceCalculationListItem(
    Guid CalculationId,
    string Title,
    string Currency,
    CatalogPriceCalculationStatus Status,
    decimal TotalAmount,
    int LinesCount,
    int ManufacturersCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ArchivedAtUtc);

public sealed record CatalogPriceCalculationsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceCalculationListItem> Items);