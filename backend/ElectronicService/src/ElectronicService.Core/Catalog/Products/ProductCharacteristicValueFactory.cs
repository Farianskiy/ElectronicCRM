using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Characteristics.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products;

internal static class ProductCharacteristicValueFactory
{
    public static Result<CharacteristicValue, DomainError> Create(
        string characteristicCode,
        CharacteristicDataType dataType,
        string value)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => CreateText(characteristicCode, value),
            CharacteristicDataType.Number => CreateNumber(value),
            CharacteristicDataType.Boolean => CreateBoolean(value),
            _ => GeneralErrors.ValueIsInvalid(nameof(dataType))
        };
    }

    private static Result<CharacteristicValue, DomainError> CreateText(
        string characteristicCode,
        string value)
    {
        if (string.Equals(
                characteristicCode,
                CatalogPoleConfigurationNormalizer.CharacteristicCode,
                StringComparison.Ordinal))
        {
            return CatalogPoleConfigurationNormalizer.TryNormalize(
                value,
                out var normalizedValue)
                ? CharacteristicValue.CreateText(normalizedValue)
                : GeneralErrors.ValueIsInvalid(nameof(value));
        }

        return CharacteristicValue.CreateText(value);
    }

    private static Result<CharacteristicValue, DomainError> CreateNumber(string value)
    {
        var normalizedValue = value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);

        var parsed = decimal.TryParse(
            normalizedValue,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var numberValue);

        return parsed
            ? CharacteristicValue.CreateNumber(numberValue)
            : GeneralErrors.ValueIsInvalid(nameof(value));
    }

    private static Result<CharacteristicValue, DomainError> CreateBoolean(string value)
    {
        var normalizedValue = value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);

        if (normalizedValue is "TRUE" or "ДА" or "ЕСТЬ" or "1" or "+")
        {
            return CharacteristicValue.CreateBoolean(true);
        }

        if (normalizedValue is "FALSE" or "НЕТ" or "ОТСУТСТВУЕТ" or "0" or "-")
        {
            return CharacteristicValue.CreateBoolean(false);
        }

        return GeneralErrors.ValueIsInvalid(nameof(value));
    }
}
