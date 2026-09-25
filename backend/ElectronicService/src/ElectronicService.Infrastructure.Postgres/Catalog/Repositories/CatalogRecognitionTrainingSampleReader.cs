using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionTrainingSampleReader : ICatalogRecognitionTrainingSampleReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionTrainingSampleReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CatalogRecognitionTrainingSampleSet> ReadAsync(CatalogRecognitionTrainingScope scope, int limit = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (scope.ManufacturerId == Guid.Empty)
        {
            throw new ArgumentException("Не указан производитель.", nameof(scope));
        }

        if (scope.ProductTypeId == Guid.Empty)
        {
            throw new ArgumentException("Не указан тип товара.", nameof(scope));
        }

        if (scope.CharacteristicDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Не указана характеристика.", nameof(scope));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 1000);

        var samples = await _dbContext.CatalogRecognitionTrainingExamples
            .AsNoTracking()
            .ConfirmedExamples(_dbContext.CatalogRecognitionFeedbackEntries).Where(x => !x.IsEvaluationOnly)
            .InScope(scope.ManufacturerId, scope.ProductTypeId, scope.CharacteristicDefinitionId)
            .OrderBy(example => example.ConfirmedAtUtc)
            .ThenBy(example => example.Id)
            .Select(example => new CatalogRecognitionTrainingSample(
                example.Id,
                example.SourceFeedbackId,
                example.ProductName,
                example.RawValue,
                example.NormalizedValue,
                example.SpanStart,
                example.SpanLength,
                example.ConfirmedAtUtc))
            .Take(limit + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMore = samples.Length > limit;
        var selectedSamples = samples;

        if (hasMore)
        {
            selectedSamples = samples.Take(limit).ToArray();
        }

        return new CatalogRecognitionTrainingSampleSet(scope, selectedSamples, hasMore);
    }
}
