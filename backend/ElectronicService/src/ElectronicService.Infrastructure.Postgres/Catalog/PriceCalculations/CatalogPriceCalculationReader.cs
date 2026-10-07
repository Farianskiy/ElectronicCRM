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

        var storedLines = await (
                from line in _dbContext.CatalogPriceCalculationLines.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking()
                    on line.ProductId equals product.Id
                where line.CalculationId == calculationId
                orderby line.CreatedAtUtc, line.Id
                select new StoredCalculationLine(
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

        var lineIds = storedLines.Select(line => line.LineId).ToArray();

        var storedComponents = await _dbContext.CatalogPriceCalculationLineComponents
            .AsNoTracking()
            .Where(component => lineIds.Contains(component.CalculationLineId))
            .OrderBy(component => component.NeedName)
            .ThenBy(component => component.Name)
            .ThenBy(component => component.Id)
            .Select(component => new StoredCalculationLineComponent(
                component.CalculationLineId,
                new CatalogPriceCalculationLineComponentDetails(
                    component.Id,
                    component.NeedDefinitionId,
                    component.NeedName,
                    component.ComponentProductId,
                    component.ManufacturerId,
                    component.ManufacturerName,
                    component.Article,
                    component.Name,
                    component.SelectionSource,
                    component.QuantityPerUnit,
                    component.TotalQuantity,
                    component.BasePriceAmount,
                    component.DiscountPercent,
                    component.ProjectPriceAmount,
                    component.TotalAmount,
                    component.CreatedAtUtc,
                    component.UpdatedAtUtc)))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var projectProductIds = storedLines
            .Select(line => line.ProductId)
            .Concat(storedComponents.Select(component =>
                component.Details.ComponentProductId))
            .Distinct()
            .ToArray();

        var productTypeReferences = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where projectProductIds.Contains(product.Id)
                select new
                {
                    ProductId = product.Id,
                    ProductTypeId = productType.Id,
                    productType.Code,
                    productType.Name
                })
            .ToDictionaryAsync(
                item => item.ProductId,
                cancellationToken)
            .ConfigureAwait(false);

        var projectProductTypeIds = productTypeReferences.Values
            .Select(item => item.ProductTypeId)
            .Distinct()
            .ToArray();

        var characteristicDefinitions = await (
                from relation in _dbContext.ProductTypeCharacteristics.AsNoTracking()
                join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                    on relation.CharacteristicDefinitionId equals definition.Id
                where projectProductTypeIds.Contains(relation.ProductTypeId)
                select new
                {
                    relation.ProductTypeId,
                    DefinitionId = definition.Id,
                    definition.Code,
                    definition.Name,
                    DataType = definition.DataType.ToString(),
                    definition.Unit,
                    relation.IsRequired
                })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var characteristicValues = await _dbContext.ProductCharacteristics
            .AsNoTracking()
            .Where(item => projectProductIds.Contains(item.ProductId))
            .Select(item => new
            {
                item.ProductId,
                DefinitionId = item.CharacteristicDefinitionId,
                item.Value.DataType,
                item.Value.TextValue,
                item.Value.NumberValue,
                item.Value.BooleanValue
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var characteristicValuesByKey = characteristicValues.ToDictionary(
            item => (item.ProductId, item.DefinitionId),
            item => item.DataType switch
            {
                ElectronicService.Domain.Catalog.Characteristics.CharacteristicDataType.Text =>
                    item.TextValue,
                ElectronicService.Domain.Catalog.Characteristics.CharacteristicDataType.Number =>
                    item.NumberValue?.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                ElectronicService.Domain.Catalog.Characteristics.CharacteristicDataType.Boolean =>
                    item.BooleanValue?.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                _ => null
            });

        IReadOnlyList<CatalogPriceCalculationProductCharacteristicDetails>
            GetCharacteristics(Guid productId)
        {
            if (!productTypeReferences.TryGetValue(productId, out var productType))
            {
                return [];
            }

            return characteristicDefinitions
                .Where(item => item.ProductTypeId == productType.ProductTypeId)
                .OrderByDescending(item => item.IsRequired)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(item =>
                    new CatalogPriceCalculationProductCharacteristicDetails(
                        item.Code,
                        item.Name,
                        item.DataType,
                        item.Unit,
                        item.IsRequired,
                        characteristicValuesByKey.GetValueOrDefault(
                            (productId, item.DefinitionId))))
                .ToArray();
        }

        var componentsByLineId = storedComponents
            .GroupBy(component => component.CalculationLineId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CatalogPriceCalculationLineComponentDetails>)group
                    .Select(component =>
                    {
                        var details = component.Details;
                        productTypeReferences.TryGetValue(
                            details.ComponentProductId,
                            out var productType);

                        return details with
                        {
                            ProductTypeCode = productType?.Code ?? string.Empty,
                            ProductTypeName = productType?.Name ?? string.Empty,
                            Characteristics = GetCharacteristics(
                                details.ComponentProductId)
                        };
                    })
                    .ToArray());

        var lines = storedLines
            .Select(line =>
            {
                var components = componentsByLineId.GetValueOrDefault(
                    line.LineId,
                    Array.Empty<CatalogPriceCalculationLineComponentDetails>());

                var productTotalAmount = decimal.Round(
                    line.ProjectPriceAmount * line.Quantity,
                    2,
                    MidpointRounding.AwayFromZero);

                var componentsTotalAmount = decimal.Round(
                    components.Sum(component => component.TotalAmount),
                    2,
                    MidpointRounding.AwayFromZero);

                productTypeReferences.TryGetValue(
                    line.ProductId,
                    out var productType);

                return new CatalogPriceCalculationLineDetails(
                    line.LineId,
                    line.ProductId,
                    line.ManufacturerId,
                    line.ManufacturerName,
                    line.PriceListId,
                    line.PriceListRowId,
                    line.Article,
                    line.Name,
                    line.Unit,
                    line.Quantity,
                    line.StockQuantity,
                    line.ShortageQuantity,
                    line.BasePriceAmount,
                    line.MrcPriceAmount,
                    line.DiscountPercent,
                    line.ProjectPriceAmount,
                    productTotalAmount,
                    componentsTotalAmount,
                    line.TotalAmount,
                    line.CreatedAtUtc,
                    line.UpdatedAtUtc,
                    components,
                    productType?.Code ?? string.Empty,
                    productType?.Name ?? string.Empty,
                    GetCharacteristics(line.ProductId));
            })
            .ToArray();

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

    private sealed record StoredCalculationLine(
        Guid LineId,
        Guid ProductId,
        Guid ManufacturerId,
        string ManufacturerName,
        Guid? PriceListId,
        Guid? PriceListRowId,
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

    private sealed record StoredCalculationLineComponent(
        Guid CalculationLineId,
        CatalogPriceCalculationLineComponentDetails Details);
}
