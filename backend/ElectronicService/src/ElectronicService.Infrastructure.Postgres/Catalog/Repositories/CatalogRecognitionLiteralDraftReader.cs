using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionLiteralDraftReader : ICatalogRecognitionLiteralDraftReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionLiteralDraftReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionLiteralDraftPage> GetPageAsync(CatalogRecognitionTrainingScope scope, int page, int pageSize, CancellationToken cancellationToken = default)
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

        var query = _dbContext.CatalogRecognitionLiteralDrafts.AsNoTracking()
            .Where(draft => draft.ManufacturerId == scope.ManufacturerId && draft.ProductTypeId == scope.ProductTypeId && draft.CharacteristicDefinitionId == scope.CharacteristicDefinitionId)
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .ThenByDescending(draft => draft.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1);

        var items = await SelectDrafts(query).ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var hasMore = items.Length > pageSize;

        return new CatalogRecognitionLiteralDraftPage(items.Take(pageSize).ToArray(), page, pageSize, hasMore);
    }

    public async Task<CatalogRecognitionLiteralDraftDetails?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogRecognitionLiteralDrafts.AsNoTracking().Where(item => item.Id == draftId);
        var draft = await SelectDrafts(query).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        var evidenceQuery = from link in _dbContext.CatalogRecognitionLiteralDraftEvidenceEntries.AsNoTracking()
                            join example in _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking() on link.TrainingExampleId equals example.Id
                            where link.DraftId == draftId
                            orderby link.IsSupporting descending, example.Id
                            select new CatalogRecognitionLiteralDraftEvidenceDetails(
                                example.Id,
                                example.ProductName,
                                example.RawValue,
                                example.NormalizedValue,
                                example.SpanStart,
                                example.SpanLength,
                                link.IsSupporting,
                                example.ConfirmedAtUtc,
                                example.RevokedAtUtc);

        var evidence = await evidenceQuery.ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return new CatalogRecognitionLiteralDraftDetails(draft, evidence);
    }

    private static IQueryable<CatalogRecognitionLiteralDraftListItem> SelectDrafts(IQueryable<CatalogRecognitionLiteralDraft> query)
    {
        return query.Select(draft => new CatalogRecognitionLiteralDraftListItem(
            draft.Id,
            draft.ManufacturerId,
            draft.ProductTypeId,
            draft.CharacteristicDefinitionId,
            draft.Literal,
            draft.NormalizedValue,
            draft.GeneratorVersion,
            draft.MatchedNameCount,
            draft.Evidence.Count,
            draft.Evidence.Count(evidence => evidence.IsSupporting),
            draft.CreatedAtUtc));
    }
}