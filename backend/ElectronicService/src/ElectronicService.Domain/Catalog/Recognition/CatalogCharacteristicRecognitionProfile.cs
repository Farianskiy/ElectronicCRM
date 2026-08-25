using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Recognition;

public sealed class CatalogCharacteristicRecognitionProfile : AggregateRoot
{
    public const int MinimumPriority = 1;

    public const int MaximumPriority = 10000;

    public const int ConfigurationJsonMaxLength = 16000;

    private CatalogCharacteristicRecognitionProfile(
        Guid id,
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        int priority,
        decimal minimumConfidence,
        string configurationJson,
        bool isActive,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
        : base(id)
    {
        ProductTypeId = productTypeId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        StrategyKind = strategyKind;
        Priority = priority;
        MinimumConfidence = minimumConfidence;
        ConfigurationJson = configurationJson;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private CatalogCharacteristicRecognitionProfile()
    {
    }

    public Guid ProductTypeId { get; private set; }

    public Guid CharacteristicDefinitionId { get; private set; }

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind { get; private set; }

    public int Priority { get; private set; }

    public decimal MinimumConfidence { get; private set; }

    public string ConfigurationJson { get; private set; } = "{}";

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<CatalogCharacteristicRecognitionProfile, DomainError> Create(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        int priority,
        decimal minimumConfidence,
        string configurationJson)
    {
        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        var settingsResult = ValidateAndNormalizeSettings(
            strategyKind,
            priority,
            minimumConfidence,
            configurationJson);

        if (settingsResult.IsFailure)
        {
            return Result.Failure<CatalogCharacteristicRecognitionProfile, DomainError>(
                settingsResult.Error);
        }

        var now = DateTime.UtcNow;

        var profile = new CatalogCharacteristicRecognitionProfile(
            Guid.CreateVersion7(),
            productTypeId,
            characteristicDefinitionId,
            strategyKind,
            priority,
            minimumConfidence,
            settingsResult.Value,
            isActive: true,
            now,
            now);

        return Result.Success<CatalogCharacteristicRecognitionProfile, DomainError>(
            profile);
    }

    public UnitResult<DomainError> Reconfigure(
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        int priority,
        decimal minimumConfidence,
        string configurationJson)
    {
        var settingsResult = ValidateAndNormalizeSettings(
            strategyKind,
            priority,
            minimumConfidence,
            configurationJson);

        if (settingsResult.IsFailure)
        {
            return UnitResult.Failure(settingsResult.Error);
        }

        StrategyKind = strategyKind;
        Priority = priority;
        MinimumConfidence = minimumConfidence;
        ConfigurationJson = settingsResult.Value;
        UpdatedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<DomainError>();
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Result<string, DomainError> ValidateAndNormalizeSettings(
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        int priority,
        decimal minimumConfidence,
        string configurationJson)
    {
        if (strategyKind == CatalogCharacteristicRecognitionStrategyKind.None
            || !Enum.IsDefined(strategyKind))
        {
            return Result.Failure<string, DomainError>(
                CatalogRecognitionErrors.StrategyKindIsInvalid(strategyKind));
        }

        if (priority < MinimumPriority || priority > MaximumPriority)
        {
            return Result.Failure<string, DomainError>(
                CatalogRecognitionErrors.PriorityIsOutOfRange(
                    priority,
                    MinimumPriority,
                    MaximumPriority));
        }

        if (minimumConfidence <= 0m || minimumConfidence > 1m)
        {
            return Result.Failure<string, DomainError>(
                CatalogRecognitionErrors.MinimumConfidenceIsOutOfRange(
                    minimumConfidence));
        }

        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return Result.Failure<string, DomainError>(
                GeneralErrors.ValueIsRequired(nameof(configurationJson)));
        }

        var trimmedConfigurationJson = configurationJson.Trim();

        if (trimmedConfigurationJson.Length > ConfigurationJsonMaxLength)
        {
            return Result.Failure<string, DomainError>(
                GeneralErrors.ValueIsTooLong(
                    nameof(configurationJson),
                    ConfigurationJsonMaxLength));
        }

        try
        {
            using var document = JsonDocument.Parse(trimmedConfigurationJson);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<string, DomainError>(
                    CatalogRecognitionErrors.ConfigurationJsonMustBeObject());
            }

            var normalizedConfigurationJson = JsonSerializer.Serialize(
                document.RootElement);

            return Result.Success<string, DomainError>(
                normalizedConfigurationJson);
        }
        catch (JsonException)
        {
            return Result.Failure<string, DomainError>(
                CatalogRecognitionErrors.ConfigurationJsonIsInvalid());
        }
    }
}