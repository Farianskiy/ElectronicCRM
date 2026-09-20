using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetVersionReader
    : ICatalogRecognitionRuleSetVersionReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionRuleSetVersionReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionRuleSetVersionPage> GetPageAsync(
        Guid manufacturerId,
        Guid productTypeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 10000);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 50);

        if (manufacturerId == Guid.Empty)
        {
            throw new ArgumentException("Не указан производитель.", nameof(manufacturerId));
        }

        if (productTypeId == Guid.Empty)
        {
            throw new ArgumentException("Не указан тип товара.", nameof(productTypeId));
        }

        var rows = await _dbContext.CatalogRecognitionRuleSetVersions
            .AsNoTracking()
            .Where(version =>
                version.ManufacturerId == manufacturerId &&
                version.ProductTypeId == productTypeId)
            .OrderByDescending(version => version.VersionNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(version => new
            {
                version.Id,
                version.VersionNumber,
                version.Name,
                EntryCount = version.Entries.Count,
                version.CreatedAtUtc
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Take(pageSize)
            .Select(row => new CatalogRecognitionRuleSetVersionListItem(
                row.Id,
                row.VersionNumber,
                row.Name,
                row.EntryCount,
                row.CreatedAtUtc))
            .ToArray();

        return new CatalogRecognitionRuleSetVersionPage(
            items,
            page,
            pageSize,
            rows.Length > pageSize);
    }

    public async Task<CatalogRecognitionRuleSetVersionDetails?> GetByIdAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty)
        {
            throw new ArgumentException("Не указана версия.", nameof(versionId));
        }

        var version = await _dbContext.CatalogRecognitionRuleSetVersions
            .AsNoTracking()
            .Include(item => item.Entries)
            .SingleOrDefaultAsync(item => item.Id == versionId, cancellationToken)
            .ConfigureAwait(false);

        if (version is null)
        {
            return null;
        }

        var entries = version.Entries
            .OrderBy(entry => entry.Position)
            .Select(entry => new CatalogRecognitionRuleSetVersionEntryItem(
                entry.Position,
                (int)entry.Kind,
                GetDraftId(entry)))
            .ToArray();

        return new CatalogRecognitionRuleSetVersionDetails(
            version.Id,
            version.ManufacturerId,
            version.ProductTypeId,
            version.VersionNumber,
            version.Name,
            version.CreatedAtUtc,
            entries);
    }

    private static Guid GetDraftId(CatalogRecognitionRuleSetEntry entry)
    {
        Guid? draftId = entry.Kind switch
        {
            CatalogRecognitionRuleKind.Literal => entry.LiteralDraftId,
            CatalogRecognitionRuleKind.NumericCapture => entry.IntegerDraftId,
            CatalogRecognitionRuleKind.MultipleNumericCaptures => entry.MultiIntegerDraftId,
            _ => null
        };

        if (draftId is not Guid value || value == Guid.Empty)
        {
            throw new InvalidOperationException("Состав версии содержит некорректную ссылку на шаблон.");
        }

        return value;
    }
}