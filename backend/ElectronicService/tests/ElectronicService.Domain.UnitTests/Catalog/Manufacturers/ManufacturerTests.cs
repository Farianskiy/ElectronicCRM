using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Domain.UnitTests.Catalog.Manufacturers;

public sealed class ManufacturerTests
{
    [Fact]
    public void CreateTrimsNameAndBuildsNormalizedName()
    {
        var result = Manufacturer.Create("  АкЭл ёлка  ");

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("АкЭл ёлка", result.Value.Name);
        Assert.Equal("АКЭЛ ЕЛКА", result.Value.NormalizedName);
    }

    [Fact]
    public void CreateReturnsRequiredErrorForBlankName()
    {
        var result = Manufacturer.Create(" ");

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_required", result.Error.Code);
    }

    [Fact]
    public void CreateReturnsTooLongErrorWhenNameExceedsLimit()
    {
        var result = Manufacturer.Create(new string('M', 201));

        Assert.True(result.IsFailure);
        Assert.Equal("general.value_is_too_long", result.Error.Code);
    }

    [Fact]
    public void RenameChangesNameAndNormalizedName()
    {
        var manufacturer = Manufacturer.Create("ИЭК").Value;

        var result = manufacturer.Rename("  IEK  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("IEK", manufacturer.Name);
        Assert.Equal("IEK", manufacturer.NormalizedName);
    }

    [Fact]
    public void RenameDoesNotChangeManufacturerWhenNewNameIsInvalid()
    {
        var manufacturer = Manufacturer.Create("CHINT").Value;

        var result = manufacturer.Rename(" ");

        Assert.True(result.IsFailure);
        Assert.Equal("CHINT", manufacturer.Name);
        Assert.Equal("CHINT", manufacturer.NormalizedName);
    }
}
