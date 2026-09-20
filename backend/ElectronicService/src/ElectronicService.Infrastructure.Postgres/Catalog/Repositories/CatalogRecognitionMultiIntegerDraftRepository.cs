using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionMultiIntegerDraftRepository
    : ICatalogRecognitionMultiIntegerDraftRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionMultiIntegerDraftRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Result<Guid, DomainError>> SaveAsync(
        CatalogRecognitionMultiIntegerDraft draft,
        IReadOnlyList<string> selectedProductNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(selectedProductNames);

        if (selectedProductNames.Count < 2 || selectedProductNames.Count > 200)
        {
            return new DomainError("training.invalid_data", "Выберите от 2 до 200 названий.");
        }

        if (selectedProductNames.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 2000))
        {
            return new DomainError("training.invalid_data", "Названия должны быть заполнены и содержать не более 2000 символов.");
        }

        var names = selectedProductNames.ToArray();

        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
        {
            return new DomainError("training.invalid_data", "Выбранные названия не должны повторяться.");
        }

        var characteristicIds = draft.Parts
            .Where(part => part.CharacteristicDefinitionId.HasValue)
            .Select(part => part.CharacteristicDefinitionId.GetValueOrDefault())
            .ToArray();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var currentExamples = await _dbContext.CatalogRecognitionTrainingExamples
                .AsNoTracking()
                .Where(example =>
                    example.RevokedAtUtc == null &&
                    example.ManufacturerId == draft.ManufacturerId &&
                    example.ProductTypeId == draft.ProductTypeId &&
                    characteristicIds.Contains(example.CharacteristicDefinitionId) &&
                    names.Contains(example.ProductName))
                .OrderBy(example => example.Id)
                .Select(example => new
                {
                    example.Id,
                    example.ProductName,
                    example.CharacteristicDefinitionId
                })
                .Take(16001)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var checkedIds = draft.Evidence
                .Select(evidence => evidence.TrainingExampleId)
                .ToHashSet();

            if (currentExamples.Length > 16000 ||
                !checkedIds.SetEquals(currentExamples.Select(example => example.Id)))
            {
                return new DomainError(
                    "training.conflict",
                    "Набор выбранных учебных примеров изменился. Повторите предварительный просмотр.");
            }

            var currentNames = currentExamples
                .Select(example => example.ProductName)
                .ToHashSet(StringComparer.Ordinal);

            var coveredPairs = currentExamples
                .Select(example => (example.ProductName, example.CharacteristicDefinitionId))
                .ToHashSet();

            if (!currentNames.SetEquals(names) ||
                coveredPairs.Count != names.Length * characteristicIds.Length)
            {
                return new DomainError(
                    "training.conflict",
                    "Не для каждого выбранного названия подтверждены все характеристики. Повторите проверку примеров.");
            }

            var candidates = _dbContext.CatalogRecognitionMultiIntegerDrafts
                .AsNoTracking()
                .Where(candidate =>
                    candidate.ManufacturerId == draft.ManufacturerId &&
                    candidate.ProductTypeId == draft.ProductTypeId &&
                    candidate.GeneratorVersion == draft.GeneratorVersion &&
                    candidate.MatchedNameCount == draft.MatchedNameCount &&
                    candidate.SupportingNameCount == draft.SupportingNameCount)
                .Include(candidate => candidate.Parts)
                .Include(candidate => candidate.Evidence)
                .AsSplitQuery()
                .AsAsyncEnumerable();

            var expectedParts = draft.Parts
                .OrderBy(part => part.Position)
                .Select(part => (
                    part.Position,
                    part.Literal,
                    part.CharacteristicDefinitionId,
                    part.DistinctValueCount))
                .ToArray();

            var expectedEvidence = draft.Evidence
                .Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting))
                .ToHashSet();

            Guid? existingId = null;

            await foreach (var candidate in candidates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var candidateParts = candidate.Parts
                    .OrderBy(part => part.Position)
                    .Select(part => (
                        part.Position,
                        part.Literal,
                        part.CharacteristicDefinitionId,
                        part.DistinctValueCount));

                var candidateEvidence = candidate.Evidence
                    .Select(evidence => (evidence.TrainingExampleId, evidence.IsSupporting));

                if (expectedParts.SequenceEqual(candidateParts) &&
                    expectedEvidence.SetEquals(candidateEvidence))
                {
                    existingId = candidate.Id;
                    break;
                }
            }

            if (existingId is Guid savedId)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return savedId;
            }

            _dbContext.CatalogRecognitionMultiIntegerDrafts.Add(draft);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return draft.Id;
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            _dbContext.ChangeTracker.Clear();

            return new DomainError(
                "training.conflict",
                "Данные одновременно изменились другим запросом. Повторите предварительный просмотр и сохранение.");
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } ||
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
    }
}