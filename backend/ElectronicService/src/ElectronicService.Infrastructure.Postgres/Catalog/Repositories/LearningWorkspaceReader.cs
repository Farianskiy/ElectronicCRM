using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

// Read-only projections. Release decisions remain in the existing command services.
public sealed class LearningWorkspaceReader(ElectronicDbContext db, RecognitionEvaluationAccess access)
{
    public const int PageSize = 20;
    public async Task<Result<object, DomainError>> ReadAsync(Guid manufacturerId, Guid productTypeId,
        string section, int page, string status, Guid? id, Guid? parentId, CancellationToken ct)
    {
        var authorization = await access.AuthorizeAsync(ct).ConfigureAwait(false);
        if (authorization.IsFailure) return authorization.Error;
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty || page is < 1 or > 10000 || id == Guid.Empty || parentId == Guid.Empty)
            return new DomainError("workspace.invalid", "Выберите производителя, тип и страницу от 1 до 10000.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct).ConfigureAwait(false);
        var manufacturer = await db.Manufacturers.AsNoTracking().Where(x => x.Id == manufacturerId).Select(x => x.Name).SingleOrDefaultAsync(ct);
        var productType = await db.ProductTypes.AsNoTracking().Where(x => x.Id == productTypeId).Select(x => new { x.Name, x.Code }).SingleOrDefaultAsync(ct);
        if (manufacturer is null || productType is null) return new DomainError("training.not_found", "Область не найдена.");
        var userId = authorization.Value;
        var asOfUtc = DateTime.UtcNow;
        var versions = db.CatalogRecognitionRuleSetVersions.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId);
        var candidates = db.CatalogRecognitionCandidates.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId);
        var suggestions = db.CatalogAssistantDictionarySuggestions.AsNoTracking().Where(x => x.Source == CatalogDictionarySuggestionSource.RecognitionLearning && candidates.Any(c => c.SuggestionId == x.Id));
        var literals = db.CatalogRecognitionLiteralDrafts.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId);
        var integers = db.CatalogRecognitionIntegerDrafts.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId);
        var multis = db.CatalogRecognitionMultiIntegerDrafts.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId);
        if (string.Equals(section, "overview", StringComparison.Ordinal))
        {
            var examples = db.CatalogRecognitionTrainingExamples.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId && x.ConfirmedByUserId == userId);
            var usable = examples.Where(x => x.RevokedAtUtc == null && db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc == null));
            var state = await db.CatalogRecognitionRuleSetSwitches.AsNoTracking().Where(x => x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId)
                .OrderByDescending(x => x.SequenceNumber).Select(x => new { ActiveVersionId = x.NewVersionId, x.SequenceNumber }).FirstOrDefaultAsync(ct);
            return new { asOfUtc, manufacturer, productType = productType.Name, productTypeCode = productType.Code,
                activeVersionId = state?.ActiveVersionId, sequenceNumber = state?.SequenceNumber ?? 0,
                trainingExamples = await usable.CountAsync(x => !x.IsEvaluationOnly, ct),
                controlExamples = await usable.CountAsync(x => x.IsEvaluationOnly, ct),
                revokedExamples = await examples.CountAsync(x => x.RevokedAtUtc != null, ct),
                excludedSources = await examples.Where(x => db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc != null)).Select(x => x.SourceFeedbackId).Distinct().CountAsync(ct),
                literalDrafts = await literals.CountAsync(ct), integerDrafts = await integers.CountAsync(ct), multiIntegerDrafts = await multis.CountAsync(ct),
                versions = await versions.CountAsync(ct), pendingSuggestions = await suggestions.CountAsync(x => x.Status == CatalogAssistantDictionarySuggestionStatus.Pending, ct),
                approvedSuggestions = await suggestions.CountAsync(x => x.Status == CatalogAssistantDictionarySuggestionStatus.Approved, ct) };
        }
        if (string.Equals(section, "suggestions", StringComparison.Ordinal))
        {
            CatalogAssistantDictionarySuggestionStatus? filter = null;
            if (!string.Equals(status, "All", StringComparison.Ordinal))
            {
                if (!Enum.TryParse<CatalogAssistantDictionarySuggestionStatus>(status, out var parsed) || !Enum.IsDefined(parsed))
                    return new DomainError("workspace.invalid", "Неизвестное состояние предложения.");
                filter = parsed;
            }
            var result = await new CatalogAssistantDictionarySuggestionReader(db).ReadAsync(filter, page, PageSize, ct, manufacturerId, productTypeId, id);
            if (id.HasValue && result.Items.Count == 0) return new DomainError("training.not_found", "Предложение в этой области недоступно.");
            return (object)result;
        }
        IQueryable<WorkspaceItem> query;
        switch (section)
        {
            case "versions":
                query = versions.Select(x => new WorkspaceItem { Id = x.Id, Kind = "version", Label = x.Name, Number = x.VersionNumber, CreatedAtUtc = x.CreatedAtUtc }); break;
            case "literal":
                query = literals.Select(x => new WorkspaceItem { Id = x.Id, Kind = "literal", Label = x.Literal, ParentId = x.CharacteristicDefinitionId, CreatedAtUtc = x.CreatedAtUtc }); break;
            case "integer":
                query = integers.Select(x => new WorkspaceItem { Id = x.Id, Kind = "integer", Label = x.Prefix, ParentId = x.CharacteristicDefinitionId, CreatedAtUtc = x.CreatedAtUtc }); break;
            case "multi":
                query = multis.Select(x => new WorkspaceItem { Id = x.Id, Kind = "multi", Label = "Составной числовой черновик", CreatedAtUtc = x.CreatedAtUtc }); break;
            case "batches":
                // No scope restriction: mixed batches are supported by the existing evaluator.
                query = db.CatalogImportBatches.AsNoTracking().Where(x => x.CreatedByUserId == userId && db.CatalogImportRows.Count(r => r.BatchId == x.Id) >= 1 && db.CatalogImportRows.Count(r => r.BatchId == x.Id) <= 2000)
                    .Select(x => new WorkspaceItem { Id = x.Id, Kind = "batch", Label = x.OriginalFileName, CreatedAtUtc = x.CreatedAtUtc }); break;
            case "ruleReports":
                query = db.CatalogRecognitionRuleSetReports.AsNoTracking().Where(x => x.CreatedByUserId == userId && versions.Any(v => v.Id == x.RuleSetVersionId) && (!parentId.HasValue || x.RuleSetVersionId == parentId))
                    .Select(x => new WorkspaceItem { Id = x.Id, Kind = "rules", Label = "Сохранённый отчёт правил; актуальность не проверена", ParentId = x.RuleSetVersionId, BatchId = x.BatchId,
                        BatchAvailable = db.CatalogImportBatches.Any(b => b.Id == x.BatchId && b.CreatedByUserId == userId), CreatedAtUtc = x.CompletedAtUtc }); break;
            case "dictionaryReports":
                query = db.Set<CatalogDictionaryEvaluationReport>().AsNoTracking().Where(x => x.CreatedByUserId == userId && candidates.Any(c => c.Id == x.CandidateId) && (!parentId.HasValue || x.SuggestionId == parentId))
                    .Select(x => new WorkspaceItem { Id = x.Id, Kind = "dictionary", Label = "Сохранённый отчёт словаря; актуальность не проверена", ParentId = x.SuggestionId, CreatedAtUtc = x.CreatedAtUtc }); break;
            case "examples":
                query = db.CatalogRecognitionTrainingExamples.AsNoTracking().Where(x => x.ConfirmedByUserId == userId && x.ManufacturerId == manufacturerId && x.ProductTypeId == productTypeId)
                    .Select(x => new WorkspaceItem { Id = x.Id, Kind = "example", Label = x.ProductName, CreatedAtUtc = x.ConfirmedAtUtc }); break;
            default: return new DomainError("workspace.invalid", "Неизвестный раздел.");
        }
        if (id.HasValue) query = query.Where(x => x.Id == id);
        var totalCount = await query.CountAsync(ct);
        if (id.HasValue && totalCount == 0) return new DomainError("training.not_found", "Объект в этой области недоступен.");
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);
        return new { items, totalCount, page, pageSize = PageSize, asOfUtc };
    }
}

public sealed class WorkspaceItem
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = "";
    public string Label { get; init; } = "";
    public int? Number { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? BatchId { get; init; }
    public bool BatchAvailable { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
