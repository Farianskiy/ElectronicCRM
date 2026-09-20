using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionTrainingExampleRepository : ICatalogRecognitionTrainingExampleRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionTrainingExampleRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionTrainingExample> AddOrGetActiveAsync(CatalogRecognitionTrainingExample example, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(example);

        var existing = await FindActiveAsync(example.SourceFeedbackId, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        _dbContext.CatalogRecognitionTrainingExamples.Add(example);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return example;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_recognition_training_example_active_source" })
        {
            _dbContext.Entry(example).State = EntityState.Detached;

            var concurrentExample = await FindActiveAsync(example.SourceFeedbackId, cancellationToken).ConfigureAwait(false);

            if (concurrentExample is null)
            {
                throw;
            }

            return concurrentExample;
        }
    }

    public async Task<IReadOnlyCollection<CatalogRecognitionTrainingExample>> GetActiveByFeedbackIdsAsync(IReadOnlyCollection<Guid> feedbackIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedbackIds);

        if (feedbackIds.Count == 0)
        {
            return [];
        }

        var ids = feedbackIds.Distinct().ToArray();

        return await _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking().Where(example => ids.Contains(example.SourceFeedbackId) && example.RevokedAtUtc == null).OrderBy(example => example.CharacteristicDefinitionId).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<CatalogRecognitionTrainingExample?> GetByIdAsync(Guid exampleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking().SingleOrDefaultAsync(example => example.Id == exampleId, cancellationToken);
    }

    public async Task RevokeAsync(Guid exampleId, Guid revokedByUserId, CancellationToken cancellationToken = default)
    {
        if (exampleId == Guid.Empty)
        {
            throw new ArgumentException("Не указан учебный пример.", nameof(exampleId));
        }

        if (revokedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Не указан пользователь.", nameof(revokedByUserId));
        }

        var revokedAtUtc = DateTime.UtcNow;

        await _dbContext.CatalogRecognitionTrainingExamples.Where(example => example.Id == exampleId && example.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters.SetProperty(example => example.RevokedByUserId, (Guid?)revokedByUserId).SetProperty(example => example.RevokedAtUtc, (DateTime?)revokedAtUtc), cancellationToken).ConfigureAwait(false);
    }

    private Task<CatalogRecognitionTrainingExample?> FindActiveAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        return _dbContext.CatalogRecognitionTrainingExamples.AsNoTracking().SingleOrDefaultAsync(example => example.SourceFeedbackId == feedbackId && example.RevokedAtUtc == null, cancellationToken);
    }
}