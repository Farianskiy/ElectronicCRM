using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionMultiIntegerDraftReader
    : ICatalogRecognitionMultiIntegerDraftReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionMultiIntegerDraftReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionMultiIntegerDraftPage> GetPageAsync(
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

        var drafts = await _dbContext.CatalogRecognitionMultiIntegerDrafts
            .AsNoTracking()
            .Where(draft =>
                draft.ManufacturerId == manufacturerId &&
                draft.ProductTypeId == productTypeId)
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .ThenByDescending(draft => draft.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(draft => new
            {
                draft.Id,
                draft.ManufacturerId,
                draft.ProductTypeId,
                draft.GeneratorVersion,
                draft.MatchedNameCount,
                draft.SupportingNameCount,
                CheckedExampleCount = draft.Evidence.Count,
                SupportingExampleCount = draft.Evidence.Count(evidence => evidence.IsSupporting),
                draft.CreatedAtUtc
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var visibleDrafts = drafts.Take(pageSize).ToArray();
        var ids = visibleDrafts.Select(draft => draft.Id).ToArray();

        if (ids.Length == 0)
        {
            return new CatalogRecognitionMultiIntegerDraftPage([], page, pageSize, false);
        }

        var parts = await _dbContext.CatalogRecognitionMultiIntegerDraftParts
            .AsNoTracking()
            .Where(part => ids.Contains(part.DraftId))
            .OrderBy(part => part.DraftId)
            .ThenBy(part => part.Position)
            .Select(part => new
            {
                part.DraftId,
                part.Position,
                part.Literal,
                part.CharacteristicDefinitionId,
                part.DistinctValueCount
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var partsByDraft = parts.ToLookup(
            part => part.DraftId,
            part => new CatalogRecognitionMultiIntegerDraftPartItem(
                part.Position,
                part.Literal,
                part.CharacteristicDefinitionId,
                part.DistinctValueCount));

        var items = visibleDrafts.Select(draft =>
            new CatalogRecognitionMultiIntegerDraftListItem(
                draft.Id,
                draft.ManufacturerId,
                draft.ProductTypeId,
                draft.GeneratorVersion,
                partsByDraft[draft.Id].ToArray(),
                draft.MatchedNameCount,
                draft.SupportingNameCount,
                draft.CheckedExampleCount,
                draft.SupportingExampleCount,
                draft.CreatedAtUtc)).ToArray();

        return new CatalogRecognitionMultiIntegerDraftPage(
            items,
            page,
            pageSize,
            drafts.Length > pageSize);
    }

    public async Task<CatalogRecognitionMultiIntegerDraftEvidencePage?> GetEvidenceAsync(
    Guid draftId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 10000);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 50);

        if (draftId == Guid.Empty)
        {
            throw new ArgumentException("Не указан черновик.", nameof(draftId));
        }

        var exists = await _dbContext.CatalogRecognitionMultiIntegerDrafts
            .AsNoTracking()
            .AnyAsync(draft => draft.Id == draftId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return null;
        }

        var query =
            from link in _dbContext.CatalogRecognitionMultiIntegerDraftEvidenceEntries.AsNoTracking()
            join example in _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking()
                on link.TrainingExampleId equals example.Id
            where link.DraftId == draftId
            orderby example.ProductName, example.CharacteristicDefinitionId, example.Id
            select new
            {
                ExampleId = example.Id,
                example.CharacteristicDefinitionId,
                example.ProductName,
                example.RawValue,
                example.NormalizedValue,
                example.SpanStart,
                example.SpanLength,
                link.IsSupporting,
                example.ConfirmedAtUtc,
                example.RevokedAtUtc
            };

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Take(pageSize)
            .Select(row => new CatalogRecognitionMultiIntegerDraftEvidenceItem(
                row.ExampleId,
                row.CharacteristicDefinitionId,
                row.ProductName,
                row.RawValue,
                row.NormalizedValue,
                row.SpanStart,
                row.SpanLength,
                row.IsSupporting,
                row.ConfirmedAtUtc,
                row.RevokedAtUtc))
            .ToArray();

        return new CatalogRecognitionMultiIntegerDraftEvidencePage(
            draftId,
            items,
            page,
            pageSize,
            rows.Length > pageSize);
    }

    public async Task<CatalogRecognitionMultiIntegerDraftSnapshot?> GetSnapshotAsync(
    Guid draftId,
    CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.CatalogRecognitionMultiIntegerDrafts
            .AsNoTracking()
            .Where(item => item.Id == draftId)
            .Include(item => item.Parts)
            .Include(item => item.Evidence)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        var names = await (
            from link in _dbContext.CatalogRecognitionMultiIntegerDraftEvidenceEntries.AsNoTracking()
            join example in _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking()
                on link.TrainingExampleId equals example.Id
            where link.DraftId == draftId
            select example.ProductName)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var productNames = names
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var parts = draft.Parts
            .OrderBy(part => part.Position)
            .Select(part => new CatalogRecognitionMultiIntegerPart(
                part.Literal,
                part.CharacteristicDefinitionId))
            .ToArray();

        var checkedIds = draft.Evidence
            .Select(item => item.TrainingExampleId)
            .OrderBy(id => id)
            .ToArray();

        return new CatalogRecognitionMultiIntegerDraftSnapshot(
            draft.Id,
            draft.ManufacturerId,
            draft.ProductTypeId,
            draft.GeneratorVersion,
            new CatalogRecognitionMultiIntegerPattern(parts),
            productNames,
            checkedIds);
    }
}