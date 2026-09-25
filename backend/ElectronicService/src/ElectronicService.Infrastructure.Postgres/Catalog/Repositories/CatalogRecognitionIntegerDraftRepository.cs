using System.Data;
using ElectronicService.Infrastructure.Postgres.Catalog.Queries;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionIntegerDraftRepository : ICatalogRecognitionIntegerDraftRepository
{
    private readonly ElectronicDbContext _dbContext;
    private readonly IRecognitionMutationGate _gate;

    public CatalogRecognitionIntegerDraftRepository(ElectronicDbContext dbContext, IRecognitionMutationGate gate)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _gate = gate;
    }

    public async Task<Result<Guid, DomainError>> SaveAsync(CatalogRecognitionIntegerDraft draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        await using var mutation = await _gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);

        try
        {
            var currentIds = await _dbContext.CatalogRecognitionTrainingExamples.ConfirmedExamples(_dbContext.CatalogRecognitionFeedbackEntries).Where(x => !x.IsEvaluationOnly).AsNoTracking()
                .Where(example => example.RevokedAtUtc == null && example.ManufacturerId == draft.ManufacturerId && example.ProductTypeId == draft.ProductTypeId && example.CharacteristicDefinitionId == draft.CharacteristicDefinitionId)
                .OrderBy(example => example.Id)
                .Select(example => example.Id)
                .Take(1001)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var checkedIds = draft.Evidence.Select(evidence => evidence.TrainingExampleId).ToHashSet();

            if (currentIds.Length > 1000 || !checkedIds.SetEquals(currentIds))
            {
                return new DomainError("training.conflict", "Набор учебных примеров изменился. Повторите предварительный просмотр.");
            }

            var candidates = _dbContext.CatalogRecognitionIntegerDrafts.AsNoTracking()
                .Where(candidate => candidate.ManufacturerId == draft.ManufacturerId && candidate.ProductTypeId == draft.ProductTypeId && candidate.CharacteristicDefinitionId == draft.CharacteristicDefinitionId && candidate.Prefix == draft.Prefix && candidate.GeneratorVersion == draft.GeneratorVersion && candidate.MatchedNameCount == draft.MatchedNameCount && candidate.DistinctValueCount == draft.DistinctValueCount)
                .Include(candidate => candidate.Suffixes)
                .Include(candidate => candidate.Evidence)
                .AsSplitQuery()
                .AsAsyncEnumerable();

            var expectedSuffixes = draft.Suffixes.Select(suffix => suffix.Text).ToHashSet(StringComparer.Ordinal);
            var expectedEvidence = draft.Evidence.Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting)).ToHashSet();

            await foreach (var candidate in candidates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (expectedSuffixes.SetEquals(candidate.Suffixes.Select(suffix => suffix.Text)) && expectedEvidence.SetEquals(candidate.Evidence.Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting))))
                {
                    return candidate.Id;
                }
            }

            _dbContext.CatalogRecognitionIntegerDrafts.Add(draft);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return draft.Id;
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return new DomainError("training.conflict", "Данные одновременно изменились другим запросом. Повторите предварительный просмотр и сохранение.");
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } || exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
    }
}