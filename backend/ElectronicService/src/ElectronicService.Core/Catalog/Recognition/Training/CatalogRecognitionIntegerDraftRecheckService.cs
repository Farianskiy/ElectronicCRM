using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionIntegerDraftRecheckService
{
    private readonly ICatalogRecognitionIntegerDraftReader _draftReader;
    private readonly ICatalogRecognitionTrainingSampleReader _sampleReader;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;

    public CatalogRecognitionIntegerDraftRecheckService(
        ICatalogRecognitionIntegerDraftReader draftReader,
        ICatalogRecognitionTrainingSampleReader sampleReader,
        ICatalogProductMetadataRepository metadataRepository,
        ICharacteristicDefinitionRepository definitionRepository)
    {
        _draftReader = draftReader;
        _sampleReader = sampleReader;
        _metadataRepository = metadataRepository;
        _definitionRepository = definitionRepository;
    }

    public async Task<CatalogRecognitionIntegerDraftRecheckResult?> RecheckAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        var draft = await _draftReader.GetSnapshotAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (draft is null)
        {
            return null;
        }

        var source = await _sampleReader.ReadAsync(draft.Scope, cancellationToken: cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var prepared = CatalogRecognitionTrainingSamplePreparer.Prepare(source);
        var issues = prepared.Issues.ToList();

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(draft.Scope.ManufacturerId, cancellationToken).ConfigureAwait(false);
        var productType = await _metadataRepository.GetProductTypeByIdAsync(draft.Scope.ProductTypeId, cancellationToken).ConfigureAwait(false);
        var definition = await _definitionRepository.GetByIdAsync(draft.Scope.CharacteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("ManufacturerNotFound", "Производитель сохранённого шаблона больше не найден.", Array.Empty<Guid>()));
        }

        if (productType is null || !productType.Characteristics.Any(item => item.CharacteristicDefinitionId == draft.Scope.CharacteristicDefinitionId))
        {
            issues.Add(new CatalogRecognitionTrainingIssue("CharacteristicNotAssigned", "Тип товара не найден или характеристика больше не относится к нему.", Array.Empty<Guid>()));
        }

        if (definition is null || definition.DataType != CharacteristicDataType.Number)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("NotNumericCharacteristic", "Характеристика не найдена или больше не является числовой.", Array.Empty<Guid>()));
        }

        var versionMatches = string.Equals(draft.GeneratorVersion, CatalogRecognitionIntegerAlternativesGenerator.Version, StringComparison.Ordinal);

        if (!versionMatches)
        {
            issues.Add(new CatalogRecognitionTrainingIssue("GeneratorVersionChanged", "Версия генератора изменилась. Требуется получить и проверить новое предложение.", Array.Empty<Guid>()));
        }

        if (!IsValidPattern(draft.Pattern))
        {
            issues.Add(new CatalogRecognitionTrainingIssue("InvalidStoredPattern", "Сохранённый шаблон имеет некорректную структуру.", Array.Empty<Guid>()));
        }

        CatalogRecognitionIntegerAlternativesProposal? evaluation = null;

        if (prepared.CanGenerate && issues.Count == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            evaluation = CatalogRecognitionIntegerAlternativesGenerator.Evaluate(draft.Pattern, prepared.Samples);

            if (evaluation.DistinctValueCount < 2)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("InsufficientDistinctValues", "Сохранённый шаблон поддерживают менее двух разных значений.", evaluation.SupportingExampleIds));
            }

            if (evaluation.ConflictingExampleIds.Count > 0)
            {
                issues.Add(new CatalogRecognitionTrainingIssue("ConflictingExamples", "Сохранённый шаблон не воспроизводит часть подтверждённой разметки.", evaluation.ConflictingExampleIds));
            }
        }

        var selectionComplete = !source.HasMore;
        var storedIds = draft.CheckedExampleIds.ToHashSet();
        var currentIds = source.Samples.Select(item => item.ExampleId).ToHashSet();

        Guid[] addedIds = [];
        Guid[] missingIds = [];
        var evidenceUnchanged = false;

        if (selectionComplete)
        {
            addedIds = currentIds.Except(storedIds).OrderBy(id => id).ToArray();
            missingIds = storedIds.Except(currentIds).OrderBy(id => id).ToArray();
            evidenceUnchanged = storedIds.SetEquals(currentIds);
        }

        return new CatalogRecognitionIntegerDraftRecheckResult(
            draft.Id,
            DateTime.UtcNow,
            CatalogRecognitionIntegerAlternativesGenerator.Version,
            versionMatches,
            selectionComplete,
            evidenceUnchanged,
            source.Samples.Count,
            addedIds,
            missingIds,
            evaluation,
            issues);
    }

    private static bool IsValidPattern(CatalogRecognitionIntegerAlternativesPattern pattern)
    {
        if (pattern.Prefix is null || pattern.Prefix.Length > 2000 || pattern.Suffixes is null || pattern.Suffixes.Count == 0 || pattern.Suffixes.Count > 16)
        {
            return false;
        }

        if (pattern.Suffixes.Any(suffix => suffix is null || suffix.Length > 2000 || (pattern.Prefix.Length == 0 && suffix.Length == 0)))
        {
            return false;
        }

        return pattern.Suffixes.Distinct(StringComparer.Ordinal).Count() == pattern.Suffixes.Count;
    }
}