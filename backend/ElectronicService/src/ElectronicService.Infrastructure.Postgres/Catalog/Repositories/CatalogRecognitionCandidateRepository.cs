using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionCandidateRepository : ICatalogRecognitionCandidateRepository
{
    private const int MaximumBatchSize = 5000;

    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionCandidateRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionCandidate>> GetAccumulatingCandidatesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > MaximumBatchSize)
        {
            return [];
        }

        return await _dbContext.CatalogRecognitionCandidates
            .Where(candidate =>
                candidate.Status == CatalogRecognitionCandidateStatus.Accumulating
                && candidate.SuggestionId == null)
            .OrderByDescending(candidate => candidate.CorrectedCount)
            .ThenByDescending(candidate => candidate.DistinctProductCount)
            .ThenByDescending(candidate => candidate.LastSeenAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetUnprocessedFinalizedFeedbackAsync(CatalogRecognitionFeedbackType feedbackType, int batchSize, CancellationToken cancellationToken = default)
    {
        if (feedbackType is not CatalogRecognitionFeedbackType.Accepted
            and not CatalogRecognitionFeedbackType.Corrected
            and not CatalogRecognitionFeedbackType.Rejected)
        {
            return [];
        }

        if (batchSize is < 1 or > MaximumBatchSize)
        {
            return [];
        }

        return await _dbContext.CatalogRecognitionFeedbackEntries
            .AsNoTracking()
            .Where(feedback =>
                feedback.Status == CatalogRecognitionFeedbackStatus.Finalized
                && feedback.IsTrainingEligible
                && feedback.FeedbackType == feedbackType
                && !_dbContext.CatalogRecognitionCandidateEvidenceEntries.Any(evidence => evidence.FeedbackId == feedback.Id))
            .OrderBy(feedback => feedback.FinalizedAtUtc)
            .ThenBy(feedback => feedback.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CatalogRecognitionCandidate?> GetByCandidateKeyAsync(string candidateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(candidateKey))
        {
            return null;
        }

        var trimmedCandidateKey = candidateKey.Trim();

        var localCandidate = _dbContext.CatalogRecognitionCandidates.Local.FirstOrDefault(candidate => string.Equals(candidate.CandidateKey, trimmedCandidateKey, StringComparison.Ordinal));

        if (localCandidate is not null)
        {
            return localCandidate;
        }

        return await _dbContext.CatalogRecognitionCandidates.SingleOrDefaultAsync(
    candidate => candidate.CandidateKey == trimmedCandidateKey,
    cancellationToken).ConfigureAwait(false);
    }

    public async Task<CatalogRecognitionCandidate?> GetBySuggestionIdAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        if (suggestionId == Guid.Empty)
        {
            return null;
        }

        var localCandidate = _dbContext.CatalogRecognitionCandidates.Local.FirstOrDefault(candidate => candidate.SuggestionId == suggestionId);

        if (localCandidate is not null)
        {
            return localCandidate;
        }

        return await _dbContext.CatalogRecognitionCandidates.SingleOrDefaultAsync(
            candidate => candidate.SuggestionId == suggestionId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasEvidenceForFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        if (feedbackId == Guid.Empty)
        {
            return false;
        }

        var existsLocally = _dbContext.CatalogRecognitionCandidateEvidenceEntries.Local.Any(evidence => evidence.FeedbackId == feedbackId);

        if (existsLocally)
        {
            return true;
        }

        return await _dbContext.CatalogRecognitionCandidateEvidenceEntries
            .AsNoTracking()
            .AnyAsync(evidence => evidence.FeedbackId == feedbackId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasEvidenceForNormalizedProductNameAsync(Guid candidateId, string normalizedProductName, CancellationToken cancellationToken = default)
    {
        if (candidateId == Guid.Empty || string.IsNullOrWhiteSpace(normalizedProductName))
        {
            return false;
        }

        var trimmedNormalizedProductName = normalizedProductName.Trim();

        var query =
            from evidence in _dbContext.CatalogRecognitionCandidateEvidenceEntries.AsNoTracking()
            join feedback in _dbContext.CatalogRecognitionFeedbackEntries.AsNoTracking()
                on evidence.FeedbackId equals feedback.Id
            where evidence.CandidateId == candidateId
                && feedback.NormalizedProductName == trimmedNormalizedProductName
            select evidence.Id;

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public void AddCandidate(CatalogRecognitionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        _dbContext.CatalogRecognitionCandidates.Add(candidate);
    }

    public void AddEvidence(CatalogRecognitionCandidateEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        _dbContext.CatalogRecognitionCandidateEvidenceEntries.Add(evidence);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}