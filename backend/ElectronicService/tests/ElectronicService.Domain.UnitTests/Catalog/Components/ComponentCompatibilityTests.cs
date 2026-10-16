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
        Assert.Equal(ComponentNeedRequirementMode.Optional, result.Value.RequirementMode);
        Assert.Equal(1, result.Value.DefaultQuantityPerUnit);
    }

    [Fact]
    public void NeedDefinitionStoresAssemblyRequirementAndDefaultQuantity()
    {
        var result = ComponentNeedDefinition.Create(
            Guid.NewGuid(),
            "FASTENERS",
            "Крепёж",
            ComponentNeedRequirementMode.RequiredForAssembly,
            4);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ComponentNeedRequirementMode.RequiredForAssembly,
            result.Value.RequirementMode);
        Assert.Equal(4, result.Value.DefaultQuantityPerUnit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ComponentNeedDefinition.MaximumDefaultQuantityPerUnit + 1)]
    public void NeedDefinitionRejectsInvalidDefaultQuantity(int quantity)
    {
        var result = ComponentNeedDefinition.Create(
            Guid.NewGuid(),
            "FASTENERS",
            "Крепёж",
            ComponentNeedRequirementMode.RequiredForAssembly,
            quantity);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void NeedDefinitionChangesRequirementAndDefaultQuantity()
    {
        var need = ComponentNeedDefinition.Create(
            Guid.NewGuid(),
            "FASTENERS",
            "Крепёж").Value;

        var result = need.ChangeSettings(
            ComponentNeedRequirementMode.RequiredForAssembly,
            4);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ComponentNeedRequirementMode.RequiredForAssembly,
            need.RequirementMode);
        Assert.Equal(4, need.DefaultQuantityPerUnit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ComponentNeedDefinition.MaximumDefaultQuantityPerUnit + 1)]
    public void NeedDefinitionRejectsInvalidChangedQuantity(int quantity)
    {
        var need = ComponentNeedDefinition.Create(
            Guid.NewGuid(),
            "FASTENERS",
            "Крепёж").Value;

        var result = need.ChangeSettings(
            ComponentNeedRequirementMode.RequiredForAssembly,
            quantity);

        Assert.True(result.IsFailure);
        Assert.Equal(ComponentNeedRequirementMode.Optional, need.RequirementMode);
        Assert.Equal(1, need.DefaultQuantityPerUnit);
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
            offer.ConditionGroups,
            compatibleValues));
        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
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
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>()));
    }

    [Fact]
    public void MatcherUsesOrBetweenConditionGroups()
    {
        var offer = ComponentOffer.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var seriesId = Guid.NewGuid();
        var currentId = Guid.NewGuid();

        Assert.True(offer.AddConstraint(
            seriesId,
            TestDataFactory.CreateTextValue("КТИ")).IsSuccess);
        Assert.True(offer.AddConstraint(
            currentId,
            TestDataFactory.CreateNumberValue(125)).IsSuccess);

        var secondGroup = offer.AddConditionGroup().Value;
        Assert.True(secondGroup.AddConstraint(
            seriesId,
            TestDataFactory.CreateTextValue("КМИ")).IsSuccess);
        Assert.True(secondGroup.AddConstraint(
            currentId,
            TestDataFactory.CreateNumberValue(150)).IsSuccess);

        var ktiValues = new Dictionary<Guid, CharacteristicValue>
        {
            [seriesId] = TestDataFactory.CreateTextValue("КТИ"),
            [currentId] = TestDataFactory.CreateNumberValue(125)
        };
        var kmiValues = new Dictionary<Guid, CharacteristicValue>
        {
            [seriesId] = TestDataFactory.CreateTextValue("КМИ"),
            [currentId] = TestDataFactory.CreateNumberValue(150)
        };
        var incompatibleValues = new Dictionary<Guid, CharacteristicValue>
        {
            [seriesId] = TestDataFactory.CreateTextValue("КТИ"),
            [currentId] = TestDataFactory.CreateNumberValue(150)
        };

        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            ktiValues));
        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            kmiValues));
        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            incompatibleValues));
    }

    [Fact]
    public void MatcherIncludesNumericRangeBoundaries()
    {
        var offer = ComponentOffer.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var currentId = Guid.NewGuid();

        Assert.True(offer.AddConstraint(
            currentId,
            TestDataFactory.CreateNumberValue(115),
            ComponentCompatibilityComparisonOperator.GreaterThanOrEqual).IsSuccess);
        Assert.True(offer.AddConstraint(
            currentId,
            TestDataFactory.CreateNumberValue(125),
            ComponentCompatibilityComparisonOperator.LessThanOrEqual).IsSuccess);

        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>
            {
                [currentId] = TestDataFactory.CreateNumberValue(115)
            }));
        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>
            {
                [currentId] = TestDataFactory.CreateNumberValue(120)
            }));
        Assert.True(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>
            {
                [currentId] = TestDataFactory.CreateNumberValue(125)
            }));
        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>
            {
                [currentId] = TestDataFactory.CreateNumberValue(114)
            }));
        Assert.False(ComponentCompatibilityMatcher.IsCompatible(
            offer.ConditionGroups,
            new Dictionary<Guid, CharacteristicValue>
            {
                [currentId] = TestDataFactory.CreateNumberValue(126)
            }));
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
