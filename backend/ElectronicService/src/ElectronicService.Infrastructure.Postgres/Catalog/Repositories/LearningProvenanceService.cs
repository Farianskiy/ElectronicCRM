using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Learning;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class LearningProvenanceService(ElectronicDbContext db, ICurrentUserProvider currentUser,
    IUserRepository users, IUserPermissionOverrideRepository permissions, IRecognitionMutationGate gate) : ILearningProvenance
{
    private async Task<Result<Guid, DomainError>> AuthorizeAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not Guid id || id == Guid.Empty)
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        var user = await users.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (user is null || !user.IsActive) return new DomainError("training.forbidden", "Учётная запись недоступна.");
        var overrides = await permissions.GetByUserIdAsync(id, ct).ConfigureAwait(false);
        var allowed = overrides.SingleOrDefault(x => x.PermissionCode == UserPermissionCode.DictionariesManage)?.IsAllowed
            ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);
        return allowed ? id : new DomainError("training.forbidden", "Требуется право управления справочниками.");
    }

    private Task<bool> CanExcludeAsync(CatalogRecognitionFeedback feedback, Guid userId, CancellationToken ct) =>
        feedback.IsFinalized ? Task.FromResult(feedback.ReviewedByUserId == userId) :
            db.CatalogImportBatches.AnyAsync(x => x.Id == feedback.ImportBatchId && x.CreatedByUserId == userId, ct);

    public async Task<Result<LearningProvenance, DomainError>> ReadAsync(Guid id, bool byExample, int page, CancellationToken cancellationToken)
    {
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        if (page is < 1 or > 1000000) return new DomainError("training.invalid_request", "Некорректная страница.");
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
        var feedbackId = id;
        var ownsExample = false;
        if (byExample)
        {
            var example = await db.CatalogRecognitionTrainingExamples.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
            if (example is null) return Missing();
            feedbackId = example.SourceFeedbackId;
            ownsExample = example.ConfirmedByUserId == auth.Value;
        }
        var feedback = await db.CatalogRecognitionFeedbackEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == feedbackId, cancellationToken).ConfigureAwait(false);
        if (feedback is null) return ownsExample ? new DomainError("training.source_unavailable", "Источник недоступен; связь не сохранена.") : Missing();
        var canExclude = await CanExcludeAsync(feedback, auth.Value, cancellationToken).ConfigureAwait(false);
        if (!canExclude && !await db.CatalogRecognitionTrainingExamples.AnyAsync(x => x.SourceFeedbackId == feedbackId && x.ConfirmedByUserId == auth.Value, cancellationToken).ConfigureAwait(false))
            return Missing();
        return await BuildAsync(feedback, canExclude, page, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<LearningProvenance, DomainError>> ExcludeAsync(Guid feedbackId, string reason, CancellationToken cancellationToken)
    {
        await using var mutation = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        var feedback = await db.CatalogRecognitionFeedbackEntries.SingleOrDefaultAsync(x => x.Id == feedbackId, cancellationToken).ConfigureAwait(false);
        if (feedback is null) return Missing();
        if (!await CanExcludeAsync(feedback, auth.Value, cancellationToken).ConfigureAwait(false))
            return new DomainError("training.forbidden", "Нет права исключать это наблюдение.");
        if (!feedback.ExcludedAtUtc.HasValue)
        {
            var excluded = feedback.ExcludeFromLearning(auth.Value, reason);
            if (excluded.IsFailure) return excluded.Error;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var candidates = await db.CatalogRecognitionCandidates.Where(c => db.CatalogRecognitionCandidateEvidenceEntries
                .Any(e => e.CandidateId == c.Id && e.FeedbackId == feedbackId)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            foreach (var candidate in candidates)
            {
                var remaining = from evidence in db.CatalogRecognitionCandidateEvidenceEntries
                    join source in db.CatalogRecognitionFeedbackEntries.ReviewedFeedback(DateTime.UtcNow) on evidence.FeedbackId equals source.Id
                    where evidence.CandidateId == candidate.Id select source;
                var counts = await remaining.GroupBy(x => x.FeedbackType).Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToArrayAsync(cancellationToken).ConfigureAwait(false);
                int Count(CatalogRecognitionFeedbackType type) => counts.Where(x => x.Type == type).Sum(x => x.Count);
                var distinct = await remaining.Select(x => x.NormalizedProductName).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
                candidate.RecalculateEvidence(Count(CatalogRecognitionFeedbackType.Accepted), Count(CatalogRecognitionFeedbackType.Corrected), Count(CatalogRecognitionFeedbackType.Rejected), distinct);
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        await db.Entry(feedback).ReloadAsync(cancellationToken).ConfigureAwait(false);
        var result = await BuildAsync(feedback, true, 1, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<Result<SuggestionEvidence, DomainError>> ReadSuggestionAsync(Guid suggestionId, CancellationToken cancellationToken)
    {
        var auth = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        if (auth.IsFailure) return auth.Error;
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);
        var c = await db.CatalogRecognitionCandidates.AsNoTracking().SingleOrDefaultAsync(x => x.SuggestionId == suggestionId, cancellationToken).ConfigureAwait(false);
        if (c is null) return Missing();
        var changed = await db.CatalogRecognitionCandidateEvidenceEntries.AnyAsync(e => e.CandidateId == c.Id &&
            db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == e.FeedbackId && f.ExcludedAtUtc != null), cancellationToken).ConfigureAwait(false);
        var reportId = await db.CatalogAssistantDictionarySuggestions.Where(s => s.Id == suggestionId && db.Set<ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryEvaluationReport>()
            .Any(r => r.Id == s.EvaluationReportId && r.CreatedByUserId == auth.Value)).Select(s => s.EvaluationReportId).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return new SuggestionEvidence(suggestionId, c.EvidenceRevision, CatalogRecognitionCandidateSuggestionPolicy.HasSufficientEvidence(c),
            c.OccurrenceCount, c.AcceptedCount, c.CorrectedCount, c.RejectedCount, c.DistinctProductCount, changed, reportId);
    }

    private async Task<LearningProvenance> BuildAsync(CatalogRecognitionFeedback feedback, bool canExclude, int page, CancellationToken ct)
    {
        var examples = db.CatalogRecognitionTrainingExamples.AsNoTracking().Where(x => x.SourceFeedbackId == feedback.Id);
        var exampleIds = examples.Select(x => x.Id);
        var literal = db.CatalogRecognitionLiteralDraftEvidenceEntries.Where(e => exampleIds.Contains(e.TrainingExampleId));
        var integer = db.CatalogRecognitionIntegerDraftEvidenceEntries.Where(e => exampleIds.Contains(e.TrainingExampleId));
        var multi = db.CatalogRecognitionMultiIntegerDraftEvidenceEntries.Where(e => exampleIds.Contains(e.TrainingExampleId));
        var drafts = literal.Select(e => new { Id = e.DraftId, ExampleId = e.TrainingExampleId, Kind = "Literal", Supporting = e.IsSupporting })
            .Concat(integer.Select(e => new { Id = e.DraftId, ExampleId = e.TrainingExampleId, Kind = "NumericCapture", Supporting = e.IsSupporting }))
            .Concat(multi.Select(e => new { Id = e.DraftId, ExampleId = e.TrainingExampleId, Kind = "MultipleNumericCaptures", Supporting = e.IsSupporting }));
        var entries = db.CatalogRecognitionRuleSetEntries.Where(e =>
            (e.LiteralDraftId != null && literal.Any(d => d.DraftId == e.LiteralDraftId)) ||
            (e.IntegerDraftId != null && integer.Any(d => d.DraftId == e.IntegerDraftId)) ||
            (e.MultiIntegerDraftId != null && multi.Any(d => d.DraftId == e.MultiIntegerDraftId)));
        var excludedExamples = db.CatalogRecognitionTrainingExamples.Where(x => db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc != null)).Select(x => x.Id);
        var affectedVersionIds = db.CatalogRecognitionRuleSetEntries.Where(e =>
            (e.LiteralDraftId != null && db.CatalogRecognitionLiteralDraftEvidenceEntries.Any(d => d.DraftId == e.LiteralDraftId && excludedExamples.Contains(d.TrainingExampleId))) ||
            (e.IntegerDraftId != null && db.CatalogRecognitionIntegerDraftEvidenceEntries.Any(d => d.DraftId == e.IntegerDraftId && excludedExamples.Contains(d.TrainingExampleId))) ||
            (e.MultiIntegerDraftId != null && db.CatalogRecognitionMultiIntegerDraftEvidenceEntries.Any(d => d.DraftId == e.MultiIntegerDraftId && excludedExamples.Contains(d.TrainingExampleId))))
            .Select(e => e.VersionId);
        var versions = from entry in entries join v in db.CatalogRecognitionRuleSetVersions on entry.VersionId equals v.Id
            select new { v.Id, DraftId = entry.LiteralDraftId ?? entry.IntegerDraftId ?? entry.MultiIntegerDraftId!.Value,
                entry.Kind, Active = db.CatalogRecognitionRuleSetSwitches.Where(s => s.ManufacturerId == v.ManufacturerId && s.ProductTypeId == v.ProductTypeId)
                    .OrderByDescending(s => s.SequenceNumber).Select(s => s.NewVersionId).FirstOrDefault() == v.Id, NeedsReview = affectedVersionIds.Contains(v.Id) };
        var versionIds = entries.Select(e => e.VersionId);
        var switches = db.CatalogRecognitionRuleSetSwitches.Where(s =>
            (s.NewVersionId != null && versionIds.Contains(s.NewVersionId.Value)) || (s.PreviousVersionId != null && versionIds.Contains(s.PreviousVersionId.Value)))
            .OrderByDescending(s => s.SequenceNumber).Select(s => new SwitchLink(s.Id, s.PreviousVersionId, s.NewVersionId, s.SequenceNumber));
        var candidates = db.CatalogRecognitionCandidates.AsNoTracking().Where(c => db.CatalogRecognitionCandidateEvidenceEntries.Any(e => e.CandidateId == c.Id && e.FeedbackId == feedback.Id));
        var count = await candidates.CountAsync(ct).ConfigureAwait(false);
        var candidateRows = await candidates.OrderBy(c => c.Id).Skip((page - 1) * 25).Take(25)
            .Select(c => new { Candidate = c, Changed = db.CatalogRecognitionCandidateEvidenceEntries.Any(e => e.CandidateId == c.Id && db.CatalogRecognitionFeedbackEntries.Any(f => f.Id == e.FeedbackId && f.ExcludedAtUtc != null)), Term = db.CatalogAssistantDictionarySuggestions.Where(s => s.Id == c.SuggestionId).Select(s => s.CreatedDictionaryTermId).FirstOrDefault(),
                ReportId = db.CatalogAssistantDictionarySuggestions.Where(s => s.Id == c.SuggestionId && db.Set<ElectronicService.Domain.Catalog.Dictionaries.CatalogDictionaryEvaluationReport>().Any(r => r.Id == s.EvaluationReportId && r.CreatedByUserId == currentUser.UserId)).Select(s => s.EvaluationReportId).FirstOrDefault(),
                SuggestionStatus = db.CatalogAssistantDictionarySuggestions.Where(s => s.Id == c.SuggestionId).Select(s => (ElectronicService.Domain.Catalog.Dictionaries.CatalogAssistantDictionarySuggestionStatus?)s.Status).FirstOrDefault() })
            .ToArrayAsync(ct).ConfigureAwait(false);
        var candidatePage = new ProvenancePage<CandidateLink>(candidateRows.Select(x => new CandidateLink(x.Candidate.Id, x.Candidate.SuggestionId,
            x.Term, x.Candidate.Status.ToString(), x.SuggestionStatus?.ToString(), x.Candidate.OccurrenceCount, x.Candidate.DistinctProductCount,
            CatalogRecognitionCandidateSuggestionPolicy.HasSufficientEvidence(x.Candidate), x.Candidate.EvidenceRevision, x.Changed, x.ReportId)).ToArray(), count, page);
        var sourceState = feedback.ExcludedAtUtc != null ? "SourceExcluded" : "Active";
        return new LearningProvenance(feedback.Id, canExclude, feedback.ExcludedAtUtc, feedback.ExclusionReason, feedback.ImportBatchId,
            await PageAsync(examples.OrderBy(x => x.Id).Select(x => new ExampleLink(x.Id,
                x.RevokedAtUtc != null ? "Revoked" : sourceState)), page, ct).ConfigureAwait(false),
            candidatePage, await PageAsync(drafts.OrderBy(x => x.Id).ThenBy(x => x.ExampleId).ThenBy(x => x.Kind).Select(x => new DraftLink(x.Id, x.ExampleId, x.Kind, x.Supporting)), page, ct).ConfigureAwait(false),
            await PageAsync(versions.OrderBy(x => x.Id).ThenBy(x => x.DraftId).Select(x => new VersionLink(x.Id, x.DraftId, x.Kind.ToString(), x.Active, x.NeedsReview)), page, ct).ConfigureAwait(false),
            await PageAsync(switches, page, ct).ConfigureAwait(false));
    }

    private static async Task<ProvenancePage<T>> PageAsync<T>(IQueryable<T> query, int page, CancellationToken ct) =>
        new(await query.Skip((page - 1) * 25).Take(25).ToArrayAsync(ct).ConfigureAwait(false), await query.CountAsync(ct).ConfigureAwait(false), page);
    private static DomainError Missing() => new("training.not_found", "Наблюдение не найдено или недоступно.");
}
