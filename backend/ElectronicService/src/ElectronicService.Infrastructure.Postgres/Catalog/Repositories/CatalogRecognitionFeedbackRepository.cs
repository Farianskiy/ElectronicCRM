using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionFeedbackRepository : ICatalogRecognitionFeedbackRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionFeedbackRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogRecognitionFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        if (feedbackId == Guid.Empty)
        {
            return Task.FromResult<CatalogRecognitionFeedback?>(null);
        }

        return _dbContext.CatalogRecognitionFeedbackEntries.SingleOrDefaultAsync(feedback => feedback.Id == feedbackId, cancellationToken);
    }

    public Task<CatalogRecognitionFeedback?> GetByImportRowAndCharacteristicAsync(Guid importRowId, Guid characteristicDefinitionId, CancellationToken cancellationToken = default)
    {
        if (importRowId == Guid.Empty || characteristicDefinitionId == Guid.Empty)
        {
            return Task.FromResult<CatalogRecognitionFeedback?>(null);
        }

        return _dbContext.CatalogRecognitionFeedbackEntries.SingleOrDefaultAsync(
            feedback => feedback.ImportRowId == importRowId && feedback.CharacteristicDefinitionId == characteristicDefinitionId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetByImportRowAsync(Guid importRowId, CancellationToken cancellationToken = default)
    {
        if (importRowId == Guid.Empty)
        {
            return [];
        }

        return await _dbContext.CatalogRecognitionFeedbackEntries
            .Where(feedback => feedback.ImportRowId == importRowId)
            .OrderBy(feedback => feedback.CharacteristicDefinitionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionFeedback>> GetPendingByImportBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        if (importBatchId == Guid.Empty)
        {
            return [];
        }

        return await _dbContext.CatalogRecognitionFeedbackEntries
            .Where(feedback => feedback.ImportBatchId == importBatchId && feedback.Status == CatalogRecognitionFeedbackStatus.Pending)
            .OrderBy(feedback => feedback.CreatedAtUtc)
            .ThenBy(feedback => feedback.CharacteristicDefinitionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ExistsForImportRowAndCharacteristicAsync(Guid importRowId, Guid characteristicDefinitionId, CancellationToken cancellationToken = default)
    {
        if (importRowId == Guid.Empty || characteristicDefinitionId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return _dbContext.CatalogRecognitionFeedbackEntries
            .AsNoTracking()
            .AnyAsync(
                feedback => feedback.ImportRowId == importRowId && feedback.CharacteristicDefinitionId == characteristicDefinitionId,
                cancellationToken);
    }

    public void Add(CatalogRecognitionFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        _dbContext.CatalogRecognitionFeedbackEntries.Add(feedback);
    }

    public void Remove(CatalogRecognitionFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        _dbContext.CatalogRecognitionFeedbackEntries.Remove(feedback);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}