using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Management;

public static class CatalogRecognitionProfileConfigurationValidator
{
    public static UnitResult<DomainError> Validate(CatalogCharacteristicRecognitionStrategyKind strategyKind, string configurationJson)
    {
        if (strategyKind == CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit)
        {
            var numericSettings = NumericWithUnitRecognitionSettingsParser.Parse(configurationJson);

            if (numericSettings is null)
            {
                return UnitResult.Failure(CatalogRecognitionErrors.ConfigurationDoesNotMatchStrategy(strategyKind));
            }
        }

        if (strategyKind == CatalogCharacteristicRecognitionStrategyKind.BooleanAlias)
        {
            var booleanAliasSettings = BooleanAliasRecognitionSettingsParser.Parse(configurationJson);

            if (booleanAliasSettings is null)
            {
                return UnitResult.Failure(CatalogRecognitionErrors.ConfigurationDoesNotMatchStrategy(strategyKind));
            }
        }

        return UnitResult.Success<DomainError>();
    }
}