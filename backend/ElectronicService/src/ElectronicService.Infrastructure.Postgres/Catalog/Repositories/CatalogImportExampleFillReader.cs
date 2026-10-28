using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogImportExampleFillReader : ICatalogImportExampleFillReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogImportExampleFillReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogImportExampleFillSample>> GetActiveSamplesAsync(
        Guid batchId,
        IReadOnlyCollection<Guid>? sourceRowIds = null,
        CancellationToken cancellationToken = default)
    {
        var rowIds = sourceRowIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var query =
            from example in _dbContext.CatalogRecognitionTrainingExamples
                .AsNoTracking()
                .ConfirmedExamples(_dbContext.CatalogRecognitionFeedbackEntries)
            join feedback in _dbContext.CatalogRecognitionFeedbackEntries.AsNoTracking()
                on example.SourceFeedbackId equals feedback.Id
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on example.CharacteristicDefinitionId equals definition.Id
            where feedback.ImportBatchId == batchId &&
                feedback.ImportRowId.HasValue
            select new
            {
                Example = example,
                Feedback = feedback,
                Definition = definition
            };

        if (rowIds is { Length: > 0 })
        {
            query = query.Where(item =>
                rowIds.Contains(item.Feedback.ImportRowId!.Value));
        }

        return await query
            .OrderBy(item => item.Example.CharacteristicDefinitionId)
            .ThenBy(item => item.Example.RawValue)
            .ThenBy(item => item.Example.Id)
            .Select(item => new CatalogImportExampleFillSample(
                item.Example.Id,
                item.Feedback.ImportRowId.GetValueOrDefault(),
                item.Example.ManufacturerId,
                item.Example.ProductTypeId,
                item.Example.CharacteristicDefinitionId,
                item.Definition.Name,
                item.Definition.Unit,
                item.Example.ProductName,
                item.Example.RawValue,
                item.Example.NormalizedValue))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
