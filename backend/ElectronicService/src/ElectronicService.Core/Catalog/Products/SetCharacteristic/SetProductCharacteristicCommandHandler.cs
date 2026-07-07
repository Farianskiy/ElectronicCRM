using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products.SetCharacteristic;

public sealed class SetProductCharacteristicCommandHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;

    public SetProductCharacteristicCommandHandler(
        IProductRepository productRepository,
        ICatalogProductMetadataRepository metadataRepository)
    {
        _productRepository = productRepository;
        _metadataRepository = metadataRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        SetProductCharacteristicCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ProductId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.ProductId)));
        }

        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.Code)));
        }

        if (string.IsNullOrWhiteSpace(command.Value))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.Value)));
        }

        var product = await _productRepository
            .GetByIdWithDetailsAsync(command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductNotFound(command.ProductId.ToString()));
        }

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(product.ProductTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeNotFound(product.ProductTypeId.ToString()));
        }

        var definition = await _metadataRepository
            .GetCharacteristicDefinitionByCodeAsync(command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicDefinitionNotFound(command.Code));
        }

        var characteristicValueResult = CreateCharacteristicValue(
            definition.DataType,
            command.Value);

        if (characteristicValueResult.IsFailure)
        {
            return UnitResult.Failure(characteristicValueResult.Error);
        }

        var setCharacteristicResult = product.SetCharacteristic(
            productType,
            definition,
            characteristicValueResult.Value);

        if (setCharacteristicResult.IsFailure)
        {
            return UnitResult.Failure(setCharacteristicResult.Error);
        }

        await _productRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }

    private static Result<CharacteristicValue, DomainError> CreateCharacteristicValue(
        CharacteristicDataType dataType,
        string value)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => CharacteristicValue.CreateText(value),

            CharacteristicDataType.Number => CreateNumberCharacteristicValue(value),

            CharacteristicDataType.Boolean => CreateBooleanCharacteristicValue(value),

            _ => GeneralErrors.ValueIsInvalid(nameof(dataType))
        };
    }

    private static Result<CharacteristicValue, DomainError> CreateNumberCharacteristicValue(
        string value)
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

        if (!parsed)
        {
            return GeneralErrors.ValueIsInvalid(nameof(value));
        }

        return CharacteristicValue.CreateNumber(numberValue);
    }

    private static Result<CharacteristicValue, DomainError> CreateBooleanCharacteristicValue(
        string value)
    {
        var normalizedValue = NormalizeText(value);

        if (string.Equals(normalizedValue, "TRUE", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ДА", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ЕСТЬ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "1", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "+", StringComparison.Ordinal))
        {
            return CharacteristicValue.CreateBoolean(true);
        }

        if (string.Equals(normalizedValue, "FALSE", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "НЕТ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ОТСУТСТВУЕТ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "0", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "-", StringComparison.Ordinal))
        {
            return CharacteristicValue.CreateBoolean(false);
        }

        return GeneralErrors.ValueIsInvalid(nameof(value));
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}