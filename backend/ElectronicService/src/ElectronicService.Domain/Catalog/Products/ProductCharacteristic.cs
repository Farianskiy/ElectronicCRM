using CSharpFunctionalExtensions;
using ElectronicService.Domain.Abstractions;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Products;

public sealed class ProductCharacteristic : Abstractions.Entity
{
    private ProductCharacteristic(
        Guid id,
        Guid productId,
        Guid characteristicDefinitionId,
        CharacteristicValue value)
        : base(id)
    {
        ProductId = productId;
        CharacteristicDefinitionId = characteristicDefinitionId;
        Value = value;
    }

    private ProductCharacteristic()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid CharacteristicDefinitionId { get; private set; }

    public CharacteristicValue Value { get; private set; } = null!;

    public static Result<ProductCharacteristic, DomainError> Create(
        Guid productId,
        Guid characteristicDefinitionId,
        CharacteristicValue value)
    {
        if (productId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(productId));
        }

        if (characteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(characteristicDefinitionId));
        }

        return new ProductCharacteristic(
            Guid.CreateVersion7(),
            productId,
            characteristicDefinitionId,
            value);
    }

    public void ChangeValue(CharacteristicValue value)
    {
        Value = value;
    }
}