using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionIntegerDraftReader : ICatalogRecognitionIntegerDraftReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionIntegerDraftReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionIntegerDraftPage> GetPageAsync(CatalogRecognitionTrainingScope scope, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 10000);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 50);

        if (scope.ManufacturerId == Guid.Empty || scope.ProductTypeId == Guid.Empty || scope.CharacteristicDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Не указана область поиска черновиков.", nameof(scope));
        }

        var drafts = await _dbContext.CatalogRecognitionIntegerDrafts.AsNoTracking()
            .Where(draft => draft.ManufacturerId == scope.ManufacturerId && draft.ProductTypeId == scope.ProductTypeId && draft.CharacteristicDefinitionId == scope.CharacteristicDefinitionId)
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .ThenByDescending(draft => draft.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(draft => new
            {
                draft.Id,
                draft.Prefix,
                draft.GeneratorVersion,
                draft.MatchedNameCount,
                draft.DistinctValueCount,
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
            return new CatalogRecognitionIntegerDraftPage([], page, pageSize, false);
        }

        var suffixes = await _dbContext.CatalogRecognitionIntegerDraftSuffixes.AsNoTracking()
            .Where(suffix => ids.Contains(suffix.DraftId))
            .OrderBy(suffix => suffix.DraftId)
            .ThenBy(suffix => suffix.Position)
            .Select(suffix => new { suffix.DraftId, suffix.Text })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var suffixesByDraft = suffixes.ToLookup(suffix => suffix.DraftId, suffix => suffix.Text);

        var items = visibleDrafts.Select(draft => new CatalogRecognitionIntegerDraftListItem(
            draft.Id,
            draft.Prefix,
            suffixesByDraft[draft.Id].ToArray(),
            draft.GeneratorVersion,
            draft.MatchedNameCount,
            draft.DistinctValueCount,
            draft.CheckedExampleCount,
            draft.SupportingExampleCount,
            draft.CreatedAtUtc)).ToArray();

        return new CatalogRecognitionIntegerDraftPage(items, page, pageSize, drafts.Length > pageSize);
    }

    public async Task<CatalogRecognitionIntegerDraftEvidencePage?> GetEvidenceAsync(Guid draftId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 10000);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 50);

        var exists = await _dbContext.CatalogRecognitionIntegerDrafts.AsNoTracking()
            .AnyAsync(draft => draft.Id == draftId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return null;
        }

        var query = from link in _dbContext.CatalogRecognitionIntegerDraftEvidenceEntries.AsNoTracking()
                    join example in _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking() on link.TrainingExampleId equals example.Id
                    where link.DraftId == draftId
                    orderby link.IsSupporting descending, example.Id
                    select new
                    {
                        ExampleId = example.Id,
                        example.ProductName,
                        example.RawValue,
                        example.NormalizedValue,
                        example.SpanStart,
                        example.SpanLength,
                        link.IsSupporting,
                        example.ConfirmedAtUtc,
                        example.RevokedAtUtc
                    };

        var rows = await query.Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Take(pageSize).Select(row => new CatalogRecognitionIntegerDraftEvidenceItem(
            row.ExampleId,
            row.ProductName,
            row.RawValue,
            row.NormalizedValue,
            row.SpanStart,
            row.SpanLength,
            row.IsSupporting,
            row.ConfirmedAtUtc,
            row.RevokedAtUtc)).ToArray();

        return new CatalogRecognitionIntegerDraftEvidencePage(draftId, items, page, pageSize, rows.Length > pageSize);
    }

    public async Task<CatalogRecognitionIntegerDraftSnapshot?> GetSnapshotAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        var draft = await _dbContext.CatalogRecognitionIntegerDrafts.AsNoTracking()
            .Where(item => item.Id == draftId)
            .Include(item => item.Suffixes)
            .Include(item => item.Evidence)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        var scope = new CatalogRecognitionTrainingScope(draft.ManufacturerId, draft.ProductTypeId, draft.CharacteristicDefinitionId);
        var suffixes = draft.Suffixes.OrderBy(item => item.Position).Select(item => item.Text).ToArray();
        var pattern = new CatalogRecognitionIntegerAlternativesPattern(draft.Prefix, suffixes);
        var checkedIds = draft.Evidence.Select(item => item.TrainingExampleId).OrderBy(id => id).ToArray();

        return new CatalogRecognitionIntegerDraftSnapshot(draft.Id, scope, pattern, draft.GeneratorVersion, checkedIds);
    }
}