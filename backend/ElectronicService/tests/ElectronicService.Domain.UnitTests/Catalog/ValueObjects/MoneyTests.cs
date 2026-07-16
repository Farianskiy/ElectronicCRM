using System.Globalization;
using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Catalog.ValueObjects;

public sealed class MoneyTests
{
    // Проверяет создание цены и нормализацию трёхбуквенного кода валюты.
    [Fact]
    public void CreateReturnsNormalizedMoneyForValidInput()
    {
        var result = Money.Create(1_250.50m, " rub ");

        Assert.True(result.IsSuccess);
        Assert.Equal(1_250.50m, result.Value.Amount);
        Assert.Equal("RUB", result.Value.Currency);
    }

    // Проверяет, что нулевая цена является допустимым денежным значением.
    [Fact]
    public void CreateAllowsZeroAmount()
    {
        var result = Money.Create(0m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Amount);
        Assert.Equal("RUB", result.Value.Currency);
    }

    // Проверяет запрет отрицательной цены.
    [Fact]
    public void CreateReturnsInvalidErrorForNegativeAmount()
    {
        var result = Money.Create(-0.01m);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет обязательность кода валюты.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateReturnsRequiredErrorForBlankCurrency(string currency)
    {
        var result = Money.Create(100m, currency);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    // Проверяет, что код валюты должен состоять ровно из трёх символов.
    [Theory]
    [InlineData("R")]
    [InlineData("RU")]
    [InlineData("RUBL")]
    public void CreateReturnsInvalidErrorForCurrencyWithWrongLength(string currency)
    {
        var result = Money.Create(100m, currency);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет равенство денег одновременно по сумме и валюте.
    [Fact]
    public void EqualityUsesAmountAndCurrency()
    {
        var first = Money.Create(100m, "rub").Value;
        var second = Money.Create(100m, "RUB").Value;

        Assert.Equal(first, second);
    }

    // Проверяет независимое от текущей локали строковое представление цены.
    [Fact]
    public void ToStringUsesInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            var money = Money.Create(1_234.5m, "RUB").Value;

            Assert.Equal("1234.5 RUB", money.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
