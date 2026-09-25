using ElectronicService.Core.Catalog.Characteristics.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

public static class CatalogRecognitionCharacteristicValueNormalizer
{
    public static bool TryNormalizeRecognizedValue(
    CharacteristicDefinition definition,
    string rawValue,
    string? manufacturerName,
    out string normalizedValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Text:
                return CatalogCharacteristicTextValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    manufacturerName,
                    out normalizedValue);

            case CharacteristicDataType.Number:
                return CatalogCharacteristicNumericValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    out normalizedValue);

            case CharacteristicDataType.Boolean:
                return CatalogCharacteristicBooleanValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    out normalizedValue);

            default:
                normalizedValue = string.Empty;

                return false;
        }
    }

}
