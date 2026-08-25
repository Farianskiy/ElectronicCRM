using ElectronicService.Domain.Catalog.Recognition;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

public sealed partial class CatalogDataSeeder
{
    private static readonly CatalogCharacteristicRecognitionProfileSeed[] RecognitionProfiles =
    [
        new(
            "MODULAR_CIRCUIT_BREAKER",
            "BREAKING_CAPACITY",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["кА", "kA", "кA", "kА"],
              "minimum": 1,
              "maximum": 150,
              "allowDecimal": true
            }
            """),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "RATED_CURRENT",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["А", "A"],
              "minimum": 0.1,
              "maximum": 125,
              "allowDecimal": true
            }
            """),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "RATED_CURRENT",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["А", "A"],
              "minimum": 1,
              "maximum": 125,
              "allowDecimal": true
            }
            """),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "LEAKAGE_CURRENT",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["мА", "mA", "мA", "mА"],
              "minimum": 1,
              "maximum": 1000,
              "allowDecimal": false
            }
            """),

        new(
            "RCD",
            "RATED_CURRENT",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["А", "A"],
              "minimum": 1,
              "maximum": 125,
              "allowDecimal": true
            }
            """),

                new(
            "RCD",
            "LEAKAGE_CURRENT",
            CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit,
            1000,
            0.9800m,
            """
            {
              "units": ["мА", "mA", "мA", "mА"],
              "minimum": 1,
              "maximum": 1000,
              "allowDecimal": false
            }
            """),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "HAS_THERMAL_RELEASE",
            CatalogCharacteristicRecognitionStrategyKind.BooleanAlias,
            1000,
            0.9800m,
            """
            {
              "trueAliases": [
                "с ТР",
                "с тепловым расцепителем",
                "тепловой расцепитель"
              ],
              "falseAliases": [
                "без ТР",
                "нет ТР",
                "без теплового расцепителя"
              ]
            }
            """)
    ];

    private async Task SeedRecognitionProfilesAsync(CancellationToken cancellationToken)
    {
        var productTypeCodes = RecognitionProfiles
            .Select(profile => profile.ProductTypeCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var characteristicCodes = RecognitionProfiles
            .Select(profile => profile.CharacteristicCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var productTypes = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(productType => productTypeCodes.Contains(productType.Code))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var productTypesByCode = productTypes.ToDictionary(
            productType => productType.Code,
            StringComparer.Ordinal);

        var characteristicDefinitions = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .Where(characteristicDefinition => characteristicCodes.Contains(characteristicDefinition.Code))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var characteristicDefinitionsByCode = characteristicDefinitions.ToDictionary(
            characteristicDefinition => characteristicDefinition.Code,
            StringComparer.Ordinal);

        var productTypeIds = productTypes
            .Select(productType => productType.Id)
            .ToArray();

        var characteristicDefinitionIds = characteristicDefinitions
            .Select(characteristicDefinition => characteristicDefinition.Id)
            .ToArray();

        var allowedCharacteristicData = await _dbContext.ProductTypeCharacteristics
            .AsNoTracking()
            .Where(productTypeCharacteristic => productTypeIds.Contains(productTypeCharacteristic.ProductTypeId))
            .Where(productTypeCharacteristic => characteristicDefinitionIds.Contains(productTypeCharacteristic.CharacteristicDefinitionId))
            .Select(productTypeCharacteristic => new
            {
                productTypeCharacteristic.ProductTypeId,
                productTypeCharacteristic.CharacteristicDefinitionId
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var allowedCharacteristicKeys = allowedCharacteristicData
            .Select(item => new ProductTypeCharacteristicKey(
                item.ProductTypeId,
                item.CharacteristicDefinitionId))
            .ToHashSet();

        var existingProfileData = await _dbContext.CatalogCharacteristicRecognitionProfiles
            .AsNoTracking()
            .Select(profile => new
            {
                profile.ProductTypeId,
                profile.CharacteristicDefinitionId,
                profile.StrategyKind
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingProfileKeys = existingProfileData
            .Select(profile => new RecognitionProfileKey(
                profile.ProductTypeId,
                profile.CharacteristicDefinitionId,
                profile.StrategyKind))
            .ToHashSet();

        foreach (var seed in RecognitionProfiles)
        {
            if (!productTypesByCode.TryGetValue(seed.ProductTypeCode, out var productType))
            {
                throw new InvalidOperationException(
                    $"Product type '{seed.ProductTypeCode}' was not found.");
            }

            if (!characteristicDefinitionsByCode.TryGetValue(seed.CharacteristicCode, out var characteristicDefinition))
            {
                throw new InvalidOperationException(
                    $"Characteristic definition '{seed.CharacteristicCode}' was not found.");
            }

            var allowedCharacteristicKey = new ProductTypeCharacteristicKey(
                productType.Id,
                characteristicDefinition.Id);

            if (!allowedCharacteristicKeys.Contains(allowedCharacteristicKey))
            {
                throw new InvalidOperationException(
                    $"Characteristic '{seed.CharacteristicCode}' is not allowed for product type '{seed.ProductTypeCode}'.");
            }

            var profileKey = new RecognitionProfileKey(
                productType.Id,
                characteristicDefinition.Id,
                seed.StrategyKind);

            if (!existingProfileKeys.Add(profileKey))
            {
                continue;
            }

            var profileResult = CatalogCharacteristicRecognitionProfile.Create(
                productType.Id,
                characteristicDefinition.Id,
                seed.StrategyKind,
                seed.Priority,
                seed.MinimumConfidence,
                seed.ConfigurationJson);

            if (profileResult.IsFailure)
            {
                throw new InvalidOperationException(profileResult.Error.Message);
            }

            await _dbContext.CatalogCharacteristicRecognitionProfiles
                .AddAsync(profileResult.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed record ProductTypeCharacteristicKey(
        Guid ProductTypeId,
        Guid CharacteristicDefinitionId);

    private sealed record RecognitionProfileKey(
        Guid ProductTypeId,
        Guid CharacteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind StrategyKind);
}