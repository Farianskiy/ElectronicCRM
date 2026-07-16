using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Domain.UnitTests.Catalog.ValueObjects;

public sealed class StockQuantityTests
{
    // Проверяет, что положительный остаток создаётся и считается доступным.
    [Fact]
    public void CreateReturnsAvailableQuantityForPositiveValue()
    {
        var result = StockQuantity.Create(12.5m);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, result.Value.Value);
        Assert.True(result.Value.IsAvailable);
    }

    // Проверяет, что нулевой остаток допустим, но товар считается недоступным.
    [Fact]
    public void CreateReturnsUnavailableQuantityForZero()
    {
        var result = StockQuantity.Create(0m);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAvailable);
    }

    // Проверяет запрет отрицательного складского остатка.
    [Fact]
    public void CreateReturnsInvalidErrorForNegativeValue()
    {
        var result = StockQuantity.Create(-1m);

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_invalid", result.Error.Code);
    }

    // Проверяет фабричный метод создания нулевого остатка.
    [Fact]
    public void ZeroCreatesUnavailableQuantity()
    {
        var quantity = StockQuantity.Zero();

        Assert.Equal(0m, quantity.Value);
        Assert.False(quantity.IsAvailable);
    }

    // Проверяет равенство складских остатков по числовому значению.
    [Fact]
    public void EqualityUsesNumericValue()
    {
        var first = StockQuantity.Create(10m).Value;
        var second = StockQuantity.Create(10m).Value;

        Assert.Equal(first, second);
    }
}
