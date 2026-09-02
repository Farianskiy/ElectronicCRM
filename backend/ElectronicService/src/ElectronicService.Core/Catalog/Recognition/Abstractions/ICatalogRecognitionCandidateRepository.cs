using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface ICatalogRecognitionCandidateRepository
{
    Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetUnprocessedFinalizedFeedbackAsync(CatalogRecognitionFeedbackType feedbackType, int batchSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRecognitionCandidate>> GetAccumulatingCandidatesAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionCandidate?> GetByCandidateKeyAsync(string candidateKey, CancellationToken cancellationToken = default);

    Task<CatalogRecognitionCandidate?> GetBySuggestionIdAsync(Guid suggestionId, CancellationToken cancellationToken = default);

    Task<bool> HasEvidenceForFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default);

    Task<bool> HasEvidenceForNormalizedProductNameAsync(Guid candidateId, string normalizedProductName, CancellationToken cancellationToken = default);

    void AddCandidate(CatalogRecognitionCandidate candidate);

    void AddEvidence(CatalogRecognitionCandidateEvidence evidence);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}