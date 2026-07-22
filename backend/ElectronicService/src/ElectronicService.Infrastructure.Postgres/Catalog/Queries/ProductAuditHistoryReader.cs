using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Products.GetAuditHistory;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using ElectronicService.Domain.Catalog.Audit;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Queries;

public sealed class ProductAuditHistoryReader
    : IProductAuditHistoryReader
{
    private readonly ElectronicDbContext
        _dbContext;

    public ProductAuditHistoryReader(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        ProductAuditHistoryPageResult?> ReadAsync(
            Guid productId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken =
                default)
    {
        var productExists = await _dbContext
            .Set<Product>()
            .AsNoTracking()
            .AnyAsync(
                product => product.Id == productId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
        {
            return null;
        }

        var query = _dbContext
            .ProductAuditEntries
            .AsNoTracking()
            .Where(entry =>
                entry.ProductId == productId);

        var totalCount = await query
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = await query
            .OrderByDescending(entry =>
                entry.ChangedAtUtc)
            .ThenByDescending(entry =>
                entry.Id)
            .Skip(
                (pageNumber - 1)
                * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var parsedEntries = entries
            .Select(ParseEntry)
            .ToList();

        var snapshots = parsedEntries
            .SelectMany(parsedEntry =>
                new[]
                {
            parsedEntry.Before,
            parsedEntry.After
                })
            .Where(snapshot =>
                snapshot is not null)
            .Cast<ProductAuditSnapshot>()
            .ToList();

        var productTypeIds = snapshots
            .Select(snapshot =>
                snapshot.ProductTypeId)
            .ToHashSet();

        var manufacturerIds = snapshots
            .Select(snapshot =>
                snapshot.ManufacturerId)
            .ToHashSet();

        var definitionIds = snapshots
            .SelectMany(snapshot =>
                snapshot.Characteristics)
            .Select(characteristic =>
                characteristic.DefinitionId)
            .ToHashSet();

        var productTypeNames =
            await LoadProductTypeNamesAsync(
                productTypeIds,
                cancellationToken)
            .ConfigureAwait(false);

        var manufacturerNames =
            await LoadManufacturerNamesAsync(
                manufacturerIds,
                cancellationToken)
            .ConfigureAwait(false);

        var definitions =
            await LoadCharacteristicDefinitionsAsync(
                definitionIds,
                cancellationToken)
            .ConfigureAwait(false);

        var items = parsedEntries
            .Select(parsedEntry =>
            {
                IReadOnlyCollection<
                    ProductAuditHistoryChangeResult>
                    changes;

                if (!parsedEntry.BeforeIsValid
                    || !parsedEntry.AfterIsValid)
                {
                    changes =
                    [
                        new ProductAuditHistoryChangeResult(
                            "auditSnapshot",
                            "Данные аудита",

                            parsedEntry.BeforeIsValid
                                ? "Снимок прочитан"
                                : "Снимок повреждён",

                            parsedEntry.AfterIsValid
                                ? "Снимок прочитан"
                                : "Снимок повреждён")
                    ];
                }
                else
                {
                    changes =
                        ProductAuditDiffBuilder.Build(
                            parsedEntry.Before,
                            parsedEntry.After,
                            productTypeNames,
                            manufacturerNames,
                            definitions);
                }

                var entry = parsedEntry.Entry;

                return new
                    ProductAuditHistoryItemResult(
                        entry.Id,
                        entry.Operation.ToString(),
                        entry.Source.ToString(),
                        entry.SourceId,
                        entry.ChangedByUserId,
                        entry.ChangedAtUtc,
                        changes);
            })
            .ToList();

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount
                    / (double)pageSize);

        return new ProductAuditHistoryPageResult(
            productId,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            items);
    }

    private async Task<
    IReadOnlyDictionary<Guid, string>>
        LoadProductTypeNamesAsync(
            HashSet<Guid> ids,
            CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _dbContext
            .Set<ProductType>()
            .AsNoTracking()
            .Where(productType =>
                ids.Contains(productType.Id))
            .Select(productType =>
                new
                {
                    productType.Id,
                    productType.Code,
                    productType.Name
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(
            row => row.Id,
            row => $"{row.Name} ({row.Code})");
    }

    private async Task<
    IReadOnlyDictionary<Guid, string>>
        LoadManufacturerNamesAsync(
            HashSet<Guid> ids,
            CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _dbContext
            .Set<Manufacturer>()
            .AsNoTracking()
            .Where(manufacturer =>
                ids.Contains(manufacturer.Id))
            .Select(manufacturer =>
                new
                {
                    manufacturer.Id,
                    manufacturer.Name
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(
            row => row.Id,
            row => row.Name);
    }

    private async Task<IReadOnlyDictionary<
        Guid,
        ProductAuditCharacteristicMetadata>>
        LoadCharacteristicDefinitionsAsync(
            HashSet<Guid> ids,
            CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<
                Guid,
                ProductAuditCharacteristicMetadata>();
        }

        var rows = await _dbContext
            .Set<CharacteristicDefinition>()
            .AsNoTracking()
            .Where(definition =>
                ids.Contains(definition.Id))
            .Select(definition =>
                new
                {
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.Unit
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(
            row => row.Id,
            row =>
                new ProductAuditCharacteristicMetadata(
                    row.Code,
                    row.Name,
                    row.Unit));
    }

    private static ParsedProductAuditEntry ParseEntry(
    ProductAuditEntry entry)
    {
        var beforeIsValid =
            ProductAuditSnapshotSerializer
                .TryDeserialize(
                    entry.BeforeJson,
                    out var before);

        var afterIsValid =
            ProductAuditSnapshotSerializer
                .TryDeserialize(
                    entry.AfterJson,
                    out var after);

        return new ParsedProductAuditEntry(
            entry,
            beforeIsValid,
            before,
            afterIsValid,
            after);
    }

    private sealed record ParsedProductAuditEntry(
        ProductAuditEntry Entry,
        bool BeforeIsValid,
        ProductAuditSnapshot? Before,
        bool AfterIsValid,
        ProductAuditSnapshot? After);
}