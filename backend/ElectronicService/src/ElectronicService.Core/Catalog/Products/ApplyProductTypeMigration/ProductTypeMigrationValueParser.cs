using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .ApplyProductTypeMigration;

internal static class
    ProductTypeMigrationValueParser
{
    public static Result<
        CharacteristicValue,
        DomainError> Parse(
            CharacteristicDefinition definition,
            string rawValue)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        return definition.DataType switch
        {
            CharacteristicDataType.Text =>
                ParseText(
                    definition,
                    rawValue),

            CharacteristicDataType.Number =>
                ParseNumber(
                    definition,
                    rawValue),

            CharacteristicDataType.Boolean =>
                ParseBoolean(
                    definition,
                    rawValue),

            _ => Result.Failure<
                CharacteristicValue,
                DomainError>(
                    CatalogErrors
                        .ProductTypeMigrationValueIsInvalid(
                            definition.Code,
                            definition
                                .DataType
                                .ToString()))
        };
    }

    private static Result<
        CharacteristicValue,
        DomainError> ParseText(
            CharacteristicDefinition definition,
            string rawValue)
    {
        var result =
            CharacteristicValue.CreateText(
                rawValue);

        return result.IsSuccess
            ? result
            : Result.Failure<
                CharacteristicValue,
                DomainError>(
                    CatalogErrors
                        .ProductTypeMigrationValueIsInvalid(
                            definition.Code,
                            definition
                                .DataType
                                .ToString()));
    }

    private static Result<
        CharacteristicValue,
        DomainError> ParseNumber(
            CharacteristicDefinition definition,
            string rawValue)
    {
        var normalizedValue = rawValue
            .Trim()
            .Replace(
                ",",
                ".",
                StringComparison.Ordinal);

        var parsed = decimal.TryParse(
            normalizedValue,
            NumberStyles.AllowLeadingSign
                | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var numberValue);

        if (!parsed)
        {
            return Result.Failure<
                CharacteristicValue,
                DomainError>(
                    CatalogErrors
                        .ProductTypeMigrationValueIsInvalid(
                            definition.Code,
                            definition
                                .DataType
                                .ToString()));
        }

        return CharacteristicValue.CreateNumber(
            numberValue);
    }

    private static Result<
        CharacteristicValue,
        DomainError> ParseBoolean(
            CharacteristicDefinition definition,
            string rawValue)
    {
        var normalizedValue = rawValue
            .Trim()
            .ToUpperInvariant();

        bool? booleanValue = normalizedValue switch
        {
            "TRUE" or "1" or "ДА" or "YES" => true,
            "FALSE" or "0" or "НЕТ" or "NO" => false,
            _ => null
        };

        if (!booleanValue.HasValue)
        {
            return Result.Failure<
                CharacteristicValue,
                DomainError>(
                    CatalogErrors
                        .ProductTypeMigrationValueIsInvalid(
                            definition.Code,
                            definition
                                .DataType
                                .ToString()));
        }

        return CharacteristicValue.CreateBoolean(
            booleanValue.Value);
    }
}