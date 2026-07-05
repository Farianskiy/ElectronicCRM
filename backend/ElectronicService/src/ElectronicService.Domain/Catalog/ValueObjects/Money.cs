using System.Globalization;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;
using static System.Globalization.CultureInfo;

namespace ElectronicService.Domain.Catalog.ValueObjects;

public sealed class Money : ValueObject
{
    private Money()
    {
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public static Result<Money, DomainError> Create(decimal amount, string currency = "RUB")
    {
        if (amount < 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return GeneralErrors.ValueIsRequired(nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3)
        {
            return GeneralErrors.ValueIsInvalid(nameof(currency));
        }

        return new Money(amount, normalizedCurrency);
    }

    public static Money Zero(string currency = "RUB")
    {
        return new Money(0, currency.Trim().ToUpperInvariant());
    }

    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Amount} {Currency}");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}