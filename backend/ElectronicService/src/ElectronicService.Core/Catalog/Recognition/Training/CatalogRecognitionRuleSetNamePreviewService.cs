using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionRuleSetNamePreviewService
{
    private readonly ICatalogRecognitionRuleSetExecutionReader _executionReader;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;

    public CatalogRecognitionRuleSetNamePreviewService(
        ICatalogRecognitionRuleSetExecutionReader executionReader,
        ICatalogProductMetadataRepository metadataRepository,
        ICharacteristicDefinitionRepository definitionRepository)
    {
        _executionReader = executionReader;
        _metadataRepository = metadataRepository;
        _definitionRepository = definitionRepository;
    }

    public async Task<Result<CatalogRecognitionRuleSetNamePreviewResult, DomainError>> PreviewAsync(
        Guid versionId,
        Guid manufacturerId,
        Guid productTypeId,
        string productName,
        CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty ||
            manufacturerId == Guid.Empty ||
            productTypeId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите версию, производителя и тип товара.");
        }

        if (string.IsNullOrWhiteSpace(productName) || productName.Length > 2000)
        {
            return new DomainError(
                "training.invalid_request",
                "Название должно содержать от 1 до 2000 символов.");
        }

        var loaded = await _executionReader.ReadAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var snapshot = loaded.Value;

        if (snapshot.ManufacturerId != manufacturerId || snapshot.ProductTypeId != productTypeId)
        {
            return new DomainError(
                "training.scope_mismatch",
                "Название проверяется в области, отличающейся от области выбранной версии.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(
            manufacturerId,
            cancellationToken).ConfigureAwait(false);

        var productType = await _metadataRepository.GetProductTypeByIdAsync(
            productTypeId,
            cancellationToken).ConfigureAwait(false);

        if (manufacturer is null || productType is null)
        {
            return new DomainError(
                "training.not_found",
                "Производитель или тип товара больше не найден.");
        }

        var executed = CatalogRecognitionRuleSetRowExecutor.Execute(
            snapshot,
            manufacturerId,
            productTypeId,
            productName,
            cancellationToken);

        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var numericIds = snapshot.NumericRules
            .Select(rule => rule.CharacteristicDefinitionId)
            .Concat(snapshot.MultiNumericRules.SelectMany(rule =>
                rule.Pattern.Parts
                    .Where(part => part.CharacteristicDefinitionId.HasValue)
                    .Select(part => part.CharacteristicDefinitionId.GetValueOrDefault())))
            .ToHashSet();

        var allIds = snapshot.LiteralRules
            .Select(rule => rule.CharacteristicDefinitionId)
            .Concat(numericIds)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var assignedIds = productType.Characteristics
            .Select(item => item.CharacteristicDefinitionId)
            .ToHashSet();

        var definitions = new Dictionary<Guid, CharacteristicDefinition>();

        foreach (var characteristicId in allIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!assignedIds.Contains(characteristicId))
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Характеристика {characteristicId} больше не относится к типу товара.");
            }

            var definition = await _definitionRepository.GetByIdAsync(
                characteristicId,
                cancellationToken).ConfigureAwait(false);

            if (definition is null ||
                definition.DataType == CharacteristicDataType.None ||
                !Enum.IsDefined(definition.DataType))
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Характеристика {characteristicId} отсутствует или имеет неподдерживаемый тип данных.");
            }

            if (numericIds.Contains(characteristicId) &&
                definition.DataType != CharacteristicDataType.Number)
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Числовой шаблон ссылается на нечисловую характеристику {characteristicId}.");
            }

            definitions.Add(characteristicId, definition);
        }

        var candidates = executed.Value.Characteristics
            .SelectMany(item => item.Sources)
            .ToArray();

        var normalizedCandidates = new List<CatalogRecognitionRuleValueCandidate>(candidates.Length);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definition = definitions[candidate.CharacteristicDefinitionId];
            var normalized = NormalizeValue(definition, candidate.NormalizedValue);

            if (normalized.IsFailure)
            {
                return normalized.Error;
            }

            normalizedCandidates.Add(candidate with
            {
                NormalizedValue = normalized.Value
            });
        }

        var resolved = CatalogRecognitionRuleSetValueResolver.Resolve(
            productName,
            normalizedCandidates);

        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var descriptions = allIds
            .Select(id => new CatalogRecognitionRuleSetCharacteristicDescription(
                id,
                definitions[id].Name,
                definitions[id].Unit,
                definitions[id].DataType))
            .ToArray();

        return new CatalogRecognitionRuleSetNamePreviewResult(
            snapshot.VersionId,
            snapshot.VersionNumber,
            manufacturerId,
            productTypeId,
            productName,
            DateTime.UtcNow,
            resolved.Value)
        {
            CharacteristicDefinitions = descriptions
        };
    }

    private static Result<string, DomainError> NormalizeValue(
        CharacteristicDefinition definition,
        string value)
    {
        if (definition.DataType == CharacteristicDataType.Text)
        {
            return value;
        }

        if (definition.DataType == CharacteristicDataType.Boolean)
        {
            if (!bool.TryParse(value, out var flag))
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Для характеристики «{definition.Name}» ожидается нормализованное значение true или false.");
            }

            return flag ? "true" : "false";
        }

        const NumberStyles numberStyles =
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        if (!decimal.TryParse(
            value,
            numberStyles,
            CultureInfo.InvariantCulture,
            out var number))
        {
            return new DomainError(
                "training.invalid_data",
                $"Для характеристики «{definition.Name}» получено некорректное числовое значение «{value}». Ожидается число без единиц с точкой в качестве десятичного разделителя.");
        }

        return number.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);
    }
}