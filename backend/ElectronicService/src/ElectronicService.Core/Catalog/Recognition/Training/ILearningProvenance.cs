using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed record ProvenancePage<T>(IReadOnlyList<T> Items, int Total, int Page);
public sealed record ExampleLink(Guid Id, string State);
public sealed record DraftLink(Guid Id, Guid ExampleId, string Kind, bool Supporting);
public sealed record VersionLink(Guid Id, Guid DraftId, string Kind, bool Active, bool NeedsReview);
public sealed record SwitchLink(Guid Id, Guid? PreviousVersionId, Guid? NewVersionId, long SequenceNumber);
public sealed record CandidateLink(Guid Id, Guid? SuggestionId, Guid? DictionaryTermId, string Status,
    string? SuggestionStatus, int OccurrenceCount, int DistinctProductCount, bool SufficientEvidence, long Revision, bool NeedsReview, Guid? EvaluationReportId = null);
public sealed record LearningProvenance(Guid FeedbackId, bool CanExclude, DateTime? ExcludedAtUtc,
    string? ExclusionReason, Guid? ImportBatchId, ProvenancePage<ExampleLink> Examples,
    ProvenancePage<CandidateLink> Candidates, ProvenancePage<DraftLink> Drafts,
    ProvenancePage<VersionLink> Versions, ProvenancePage<SwitchLink> Switches);
public sealed record SuggestionEvidence(Guid SuggestionId, long Revision, bool SufficientEvidence,
    int OccurrenceCount, int AcceptedCount, int CorrectedCount, int RejectedCount, int DistinctProductCount, bool NeedsReview, Guid? EvaluationReportId = null);

public interface ILearningProvenance
{
    Task<Result<LearningProvenance, DomainError>> ReadAsync(Guid id, bool byExample, int page, CancellationToken cancellationToken);
    Task<Result<LearningProvenance, DomainError>> ExcludeAsync(Guid feedbackId, string reason, CancellationToken cancellationToken);
    Task<Result<SuggestionEvidence, DomainError>> ReadSuggestionAsync(Guid suggestionId, CancellationToken cancellationToken);
}
