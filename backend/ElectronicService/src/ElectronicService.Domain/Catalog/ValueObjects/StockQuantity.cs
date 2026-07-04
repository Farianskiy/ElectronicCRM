using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ValueObjects;

public sealed class StockQuantity : ValueObject
{
    private StockQuantity(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public bool IsAvailable => Value > 0;

    public static Result<StockQuantity, DomainError> Create(decimal value)
    {
        if (value < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(value));
        }

        return new StockQuantity(value);
    }

    public static StockQuantity Zero()
    {
        return new StockQuantity(0);
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}