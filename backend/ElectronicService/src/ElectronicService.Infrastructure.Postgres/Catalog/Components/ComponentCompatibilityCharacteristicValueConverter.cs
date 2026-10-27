using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Characteristics.Normalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Components;

internal static class ComponentCompatibilityCharacteristicValueConverter
{
    public static Result<CharacteristicValue, DomainError> Parse(
        CharacteristicDefinition definition,
        string rawValue)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Result<CharacteristicValue, DomainError> result = definition.DataType switch
        {
            CharacteristicDataType.Text when
                CatalogCharacteristicTextValueNormalizer.TryNormalizeToString(
                    definition.Code,
                    rawValue,
                    manufacturerName: null,
                    out var normalizedText) =>
                CharacteristicValue.CreateText(normalizedText),
            CharacteristicDataType.Number when decimal.TryParse(
                rawValue.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) => CharacteristicValue.CreateNumber(number),
            CharacteristicDataType.Boolean when
                CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                    definition.Code,
                    rawValue,
                    out var boolean) => CharacteristicValue.CreateBoolean(boolean),
            _ => Result.Failure<CharacteristicValue, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(rawValue)))
        };

        if (result.IsFailure)
        {
            return result;
        }

        var validationResult = definition.ValidateValue(result.Value);

        return validationResult.IsSuccess
            ? result
            : Result.Failure<CharacteristicValue, DomainError>(
                validationResult.Error);
    }

    public static string FormatForExcel(
        string characteristicCode,
        CharacteristicValue value)
    {
        if (value.DataType == CharacteristicDataType.Text
            && string.Equals(
                characteristicCode,
                CatalogPoleConfigurationNormalizer.CharacteristicCode,
                StringComparison.OrdinalIgnoreCase)
            && CatalogPoleConfigurationNormalizer.TryNormalize(
                value.TextValue,
                out var normalizedPoles))
        {
            return normalizedPoles switch
            {
                "1P" => "1",
                "2P" => "2",
                "3P" => "3",
                "4P" => "4",
                _ => normalizedPoles
            };
        }

        return FormatCanonical(value);
    }

    public static string FormatCanonical(CharacteristicValue value)
    {
        return value.DataType switch
        {
            CharacteristicDataType.Boolean =>
                value.BooleanValue == true ? "Да" : "Нет",
            CharacteristicDataType.Number => value.NumberValue?.ToString(
                "0.############################",
                CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.TextValue ?? string.Empty
        };
    }
}
