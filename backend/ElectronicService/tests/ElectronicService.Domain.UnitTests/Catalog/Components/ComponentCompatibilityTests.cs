using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.TestCommon;

namespace ElectronicService.Domain.UnitTests.Catalog.Components;

public sealed class ComponentCompatibilityTests
{
    [Fact]
    public void NeedDefinitionNormalizesCode()
    {
        var result = ComponentNeedDefinition.Create(
            Guid.NewGuid(),
            " mounting-panel ",
            "  Монтажная панель  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("MOUNTING_PANEL", result.Value.Code);
        Assert.Equal("Монтажная панель", result.Value.Name);
    }

    [Fact]
    public void MatcherUsesOrInsideOneCharacteristicAndAcrossCharacteristics()
    {
        var offer = ComponentOffer.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var heightId = Guid.NewGuid();
        var widthId = Guid.NewGuid();

        Assert.True(offer.AddConstraint(
            heightId,
            TestDataFactory.CreateNumberValue(1800)).IsSuccess);
        Assert.True(offer.AddConstraint(
            heightId,
            TestDataFactory.CreateNumberValue(2000)).IsSuccess);
        Assert.True(offer.AddConstraint(
            widthId,
            TestDataFactory.CreateNumberValue(800)).IsSuccess);

        var compatibleValues = new Dictionary<Guid, CharacteristicValue>
        {
            [heightId] = TestDataFactory.CreateNumberValue(2000),
            [widthId] = TestDataFactory.CreateNumberValue(800)
        };

        var incompatibleValues = new Dictionary<Guid, CharacteristicValue>
        {
            [heightId] = TestDataFactory.CreateNumberValue(2000),
            [widthId] = TestDataFactory.CreateNumberValue(600)
        };

        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.Constraints,
            compatibleValues));
        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.Constraints,
            incompatibleValues));
    }

    [Fact]
    public void MissingProductCharacteristicIsNotCompatible()
    {
        var offer = ComponentOffer.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var widthId = Guid.NewGuid();

        Assert.True(offer.AddConstraint(
            widthId,
            TestDataFactory.CreateNumberValue(800)).IsSuccess);

        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.Constraints,
            new Dictionary<Guid, CharacteristicValue>()));
    }

    [Fact]
    public void ProductNeedStatusDoesNotInferMissingFromUnknown()
    {
        var state = ProductComponentNeedState.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProductNeedStatus.Unknown);

        Assert.True(state.IsSuccess);
        Assert.Equal(ProductNeedStatus.Unknown, state.Value.Status);

        Assert.True(state.Value.ChangeStatus(ProductNeedStatus.Missing).IsSuccess);
        Assert.Equal(ProductNeedStatus.Missing, state.Value.Status);
    }

    [Fact]
    public void SelectedComponentStoresQuantity()
    {
        var result = ProductSelectedComponent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ProductSelectedComponent.MaximumQuantity + 1)]
    public void SelectedComponentRejectsInvalidQuantity(int quantity)
    {
        var result = ProductSelectedComponent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.selected_component.invalid_quantity",
            result.Error.Code);
    }

    [Fact]
    public void SelectedComponentChangesQuantity()
    {
        var selection = ProductSelectedComponent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1).Value;

        var result = selection.ChangeQuantity(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, selection.Quantity);
        Assert.NotNull(selection.UpdatedAtUtc);
    }
}
