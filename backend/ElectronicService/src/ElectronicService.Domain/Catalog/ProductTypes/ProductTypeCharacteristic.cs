using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ProductTypes;

public sealed class ProductTypeCharacteristic : Abstractions.Entity
{
    private ProductTypeCharacteristic(
        Guid id,
        Guid productTypeId,
        Guid characteristicDefinitionId,
        bool isRequired,
        bool isFilterable,
        bool isUsedForReplacement,
        ReplacementMatchMode replacementMatchMode,
        int replacementWeight)
        : base(id)
    {
        ProductTypeId = productTypeId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        IsUsedForReplacement = isUsedForReplacement;
        ReplacementMatchMode = replacementMatchMode;
        ReplacementWeight = replacementWeight;
    }

    private ProductTypeCharacteristic()
    {
    }

    public Guid ProductTypeId { get; private set; }

    public Guid CharacteristicDefinitionId { get; private set; }

    public bool IsRequired { get; private set; }

    public bool IsFilterable { get; private set; }

    public bool IsUsedForReplacement { get; private set; }

    public ReplacementMatchMode ReplacementMatchMode { get; private set; }

    public int ReplacementWeight { get; private set; }

    // Метод создаёт связь между типом товара и характеристикой
    public static Result<ProductTypeCharacteristic, DomainError> Create(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        bool isRequired,
        bool isFilterable,
        bool isUsedForReplacement,
        ReplacementMatchMode replacementMatchMode,
        int replacementWeight)
    {
        if (productTypeId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productTypeId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        if (isUsedForReplacement && replacementMatchMode == ReplacementMatchMode.None)
        {
            return GeneralErrors.ValueIsInvalid(nameof(replacementMatchMode));
        }

        if (replacementWeight < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(replacementWeight));
        }

        return new ProductTypeCharacteristic(
            Guid.CreateVersion7(),
            productTypeId,
            characteristicDefinitionId,
            isRequired,
            isFilterable,
            isUsedForReplacement,
            replacementMatchMode,
            replacementWeight);
    }

    // Метод позволяет изменить настройки подбора аналогов для этой характеристики
    /*
        Было:
        Серия производителя:
        isUsedForReplacement = false
        
        Стало:
        Серия производителя:
        isUsedForReplacement = true
        matchMode = Optional
        weight = 10
    */
    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }

    public UnitResult<DomainError> ConfigureReplacement(
        bool isUsedForReplacement,
        ReplacementMatchMode replacementMatchMode,
        int replacementWeight)
    {
        if (isUsedForReplacement && replacementMatchMode == ReplacementMatchMode.None)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(replacementMatchMode)));
        }

        if (replacementWeight < 0)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(replacementWeight)));
        }

        IsUsedForReplacement = isUsedForReplacement;
        ReplacementMatchMode = replacementMatchMode;
        ReplacementWeight = replacementWeight;

        return UnitResult.Success<DomainError>();
    }
}