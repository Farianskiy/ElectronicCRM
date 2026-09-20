using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionMultiIntegerProposalService
{
    private readonly ICatalogRecognitionTrainingSampleReader _sampleReader;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;

    public CatalogRecognitionMultiIntegerProposalService(
        ICatalogRecognitionTrainingSampleReader sampleReader,
        ICatalogProductMetadataRepository metadataRepository,
        ICharacteristicDefinitionRepository definitionRepository)
    {
        _sampleReader = sampleReader;
        _metadataRepository = metadataRepository;
        _definitionRepository = definitionRepository;
    }

    public async Task<Result<CatalogRecognitionMultiIntegerProposalSet, DomainError>> PreviewAsync(
        Guid manufacturerId,
        Guid productTypeId,
        IReadOnlyList<Guid> characteristicDefinitionIds,
        IReadOnlyList<string> productNames,
        CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите производителя и тип товара.");
        }

        if (characteristicDefinitionIds is null || characteristicDefinitionIds.Count < 2 || characteristicDefinitionIds.Count > 16)
        {
            return new DomainError("training.invalid_request", "Выберите от 2 до 16 числовых характеристик.");
        }

        if (characteristicDefinitionIds.Contains(Guid.Empty) || characteristicDefinitionIds.Distinct().Count() != characteristicDefinitionIds.Count)
        {
            return new DomainError("training.invalid_request", "Характеристики не должны повторяться или иметь пустой идентификатор.");
        }

        if (productNames is null || productNames.Count < 2 || productNames.Count > 200)
        {
            return new DomainError("training.invalid_request", "Выберите от 2 до 200 разных учебных названий.");
        }

        if (productNames.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 2000))
        {
            return new DomainError("training.invalid_request", "Названия должны быть заполнены и не превышать 2000 символов.");
        }

        var selectedNames = productNames.ToHashSet(StringComparer.Ordinal);

        if (selectedNames.Count != productNames.Count)
        {
            return new DomainError("training.invalid_request", "В учебной подборке повторяются одинаковые названия.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(manufacturerId, cancellationToken).ConfigureAwait(false);
        var productType = await _metadataRepository.GetProductTypeByIdAsync(productTypeId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null || productType is null)
        {
            return new DomainError("training.not_found", "Производитель или тип товара не найден.");
        }

        var orderedCharacteristicIds = characteristicDefinitionIds.OrderBy(id => id).ToArray();

        foreach (var characteristicId in orderedCharacteristicIds)
        {
            if (!productType.Characteristics.Any(item => item.CharacteristicDefinitionId == characteristicId))
            {
                return new DomainError("training.invalid_request", $"Характеристика {characteristicId} не относится к выбранному типу товара.");
            }

            var definition = await _definitionRepository.GetByIdAsync(characteristicId, cancellationToken).ConfigureAwait(false);

            if (definition is null || definition.DataType != CharacteristicDataType.Number)
            {
                return new DomainError("training.invalid_request", $"Характеристика {characteristicId} не найдена или не является числовой.");
            }
        }

        var sources = new List<CatalogRecognitionTrainingSampleSet>();
        var issues = new List<CatalogRecognitionTrainingIssue>();

        foreach (var characteristicId in orderedCharacteristicIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scope = new CatalogRecognitionTrainingScope(manufacturerId, productTypeId, characteristicId);
            var source = await _sampleReader.ReadAsync(scope, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (source.HasMore)
            {
                return new DomainError("training.selection_too_large", $"Для характеристики {characteristicId} превышен текущий лимит чтения подтверждений. Генерация по обрезанной выборке запрещена.");
            }

            var selectedSamples = source.Samples
                .Where(sample => selectedNames.Contains(sample.ProductName))
                .ToArray();

            var availableNames = selectedSamples.Select(sample => sample.ProductName).ToHashSet(StringComparer.Ordinal);
            var missingNames = selectedNames.Except(availableNames, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal);

            foreach (var missingName in missingNames)
            {
                issues.Add(new CatalogRecognitionTrainingIssue(
                    "MissingSelectedConfirmation",
                    $"Для названия «{missingName}» нет действующего подтверждения характеристики {characteristicId}.",
                    Array.Empty<Guid>()));
            }

            sources.Add(new CatalogRecognitionTrainingSampleSet(scope, selectedSamples, false));
        }

        if (issues.Count > 0)
        {
            return new CatalogRecognitionMultiIntegerProposalSet(
                manufacturerId,
                productTypeId,
                CatalogRecognitionMultiIntegerProposalGenerator.Version,
                [],
                issues);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var prepared = CatalogRecognitionMultiIntegerSamplePreparer.Prepare(manufacturerId, productTypeId, sources);
        var generated = CatalogRecognitionMultiIntegerProposalGenerator.Generate(prepared, cancellationToken);

        var checkedExampleIds = sources
            .SelectMany(source => source.Samples)
            .Select(sample => sample.ExampleId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        return generated with { CheckedExampleIds = checkedExampleIds };
    }
}