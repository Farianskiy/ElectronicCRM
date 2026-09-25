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

public sealed class CatalogRecognitionLiteralDraftRepository : ICatalogRecognitionLiteralDraftRepository
{
    private readonly ElectronicDbContext _dbContext;
    private readonly IRecognitionMutationGate _gate;

    public CatalogRecognitionLiteralDraftRepository(ElectronicDbContext dbContext, IRecognitionMutationGate gate)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _gate = gate;
    }

    public async Task<Result<Guid, DomainError>> SaveAsync(CatalogRecognitionLiteralDraft draft, CancellationToken cancellationToken = default)
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

            var existing = await _dbContext.CatalogRecognitionLiteralDrafts.AsNoTracking()
                .Include(candidate => candidate.Evidence)
                .Where(candidate => candidate.ManufacturerId == draft.ManufacturerId && candidate.ProductTypeId == draft.ProductTypeId && candidate.CharacteristicDefinitionId == draft.CharacteristicDefinitionId && candidate.Literal == draft.Literal && candidate.NormalizedValue == draft.NormalizedValue && candidate.GeneratorVersion == draft.GeneratorVersion && candidate.MatchedNameCount == draft.MatchedNameCount)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var expectedEvidence = draft.Evidence.Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting)).ToHashSet();

            var identical = existing.FirstOrDefault(candidate => expectedEvidence.SetEquals(candidate.Evidence.Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting))));

            if (identical is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return identical.Id;
            }

            _dbContext.CatalogRecognitionLiteralDrafts.Add(draft);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return draft.Id;
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            return new DomainError("training.conflict", "Данные одновременно изменились другим запросом. Повторите сохранение.");
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } || exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
    }
}