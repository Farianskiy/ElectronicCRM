using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogRecognitionCandidateEvidence : ElectronicService.Domain.Abstractions.Entity
{
    private CatalogRecognitionCandidateEvidence(Guid id, Guid candidateId, Guid feedbackId, DateTime createdAtUtc)
        : base(id)
    {
        CandidateId = candidateId;
        FeedbackId = feedbackId;
        CreatedAtUtc = createdAtUtc;
    }

    private CatalogRecognitionCandidateEvidence()
    {
    }

    public Guid CandidateId { get; private set; }

    public Guid FeedbackId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<CatalogRecognitionCandidateEvidence, DomainError> Create(Guid candidateId, Guid feedbackId)
    {
        if (candidateId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(candidateId));
        }

        if (feedbackId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(feedbackId));
        }

        return new CatalogRecognitionCandidateEvidence(Guid.CreateVersion7(), candidateId, feedbackId, DateTime.UtcNow);
    }
}