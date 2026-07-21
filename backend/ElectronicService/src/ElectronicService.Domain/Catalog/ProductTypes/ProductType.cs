using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ProductTypes;

public sealed class ProductType : AggregateRoot
{
    private const int CodeMaxLength = 100;
    private const int NameMaxLength = 200;

    private readonly List<ProductTypeCharacteristic> _characteristics = [];

    private ProductType(
        Guid id,
        string code,
        string name)
        : base(id)
    {
        Code = code;
        Name = name;
    }

    private ProductType()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<ProductTypeCharacteristic> Characteristics => _characteristics;

    public static Result<ProductType, DomainError> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired(nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return GeneralErrors.ValueIsRequired(nameof(name));
        }

        var normalizedCode = NormalizeCode(code);
        var normalizedName = name.Trim();

        if (normalizedCode.Length > CodeMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(code), CodeMaxLength);
        }

        if (normalizedName.Length > NameMaxLength)
        {
            return GeneralErrors.ValueIsTooLong(nameof(name), NameMaxLength);
        }

        return new ProductType(
            Guid.CreateVersion7(),
            normalizedCode,
            normalizedName);
    }

    // Метод добавляет характеристику к типу товара
    public UnitResult<DomainError> AddCharacteristic(
        CharacteristicDefinition definition,
        bool isRequired,
        bool isFilterable,
        bool isUsedForReplacement,
        ReplacementMatchMode replacementMatchMode,
        int replacementWeight)
    {
        if (_characteristics.Any(x => x.CharacteristicDefinitionId == definition.Id))
        {
            return UnitResult.Failure(
                CatalogErrors.CharacteristicAlreadyAddedToProductType(definition.Code));
        }

        var characteristicResult = ProductTypeCharacteristic.Create(
            Id,
            definition.Id,
            isRequired,
            isFilterable,
            isUsedForReplacement,
            replacementMatchMode,
            replacementWeight);

        if (characteristicResult.IsFailure)
        {
            return UnitResult.Failure(characteristicResult.Error);
        }

        _characteristics.Add(characteristicResult.Value);

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> SetCharacteristicRequired(
    Guid characteristicDefinitionId,
    bool isRequired)
    {
        if (characteristicDefinitionId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(characteristicDefinitionId)));
        }

        var characteristic = _characteristics
            .FirstOrDefault(item =>
                item.CharacteristicDefinitionId
                    == characteristicDefinitionId);

        if (characteristic is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeCharacteristicNotFound(
                    Id,
                    characteristicDefinitionId));
        }

        characteristic.SetRequired(isRequired);

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> RemoveCharacteristic(
    Guid characteristicDefinitionId)
    {
        if (characteristicDefinitionId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(
                    nameof(characteristicDefinitionId)));
        }

        var characteristic = _characteristics
            .FirstOrDefault(item =>
                item.CharacteristicDefinitionId
                    == characteristicDefinitionId);

        if (characteristic is null)
        {
            return UnitResult.Failure(
                CatalogErrors.ProductTypeCharacteristicNotFound(
                    Id,
                    characteristicDefinitionId));
        }

        _characteristics.Remove(characteristic);

        return UnitResult.Success<DomainError>();
    }

    // Метод для проверки, разрешена ли характеристика для типа товара
    public bool AllowsCharacteristic(Guid characteristicDefinitionId)
    {
        return _characteristics.Any(x => x.CharacteristicDefinitionId == characteristicDefinitionId);
    }

    // Метод возвращает список идентификаторов обязательных характеристик для типа товара
    public bool IsCharacteristicRequired(Guid characteristicDefinitionId)
    {
        return _characteristics.Any(x =>
            x.IsRequired &&
            x.CharacteristicDefinitionId == characteristicDefinitionId);
    }

    public Guid? FindMissingRequiredCharacteristicId(
        IReadOnlySet<Guid> existingCharacteristicDefinitionIds)
    {
        var missingRequiredCharacteristic = _characteristics
            .Where(x => x.IsRequired)
            .FirstOrDefault(x =>
                !existingCharacteristicDefinitionIds.Contains(x.CharacteristicDefinitionId));

        return missingRequiredCharacteristic?.CharacteristicDefinitionId;
    }

    private static string NormalizeCode(string code)
    {
        return code
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}