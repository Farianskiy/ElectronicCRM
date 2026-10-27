using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Domain.UnitTests.Catalog.ImportBatches;

public sealed class CatalogImportBatchModeTests
{
    [Fact]
    public void CreateAcceptsCreateMissingAndUpdateCharacteristicsMode()
    {
        var result = CatalogImportBatch.Create(
            Guid.NewGuid(),
            "mixed.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [1],
            CatalogImportMode.CreateMissingAndUpdateCharacteristics);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            CatalogImportMode.CreateMissingAndUpdateCharacteristics,
            result.Value.ImportMode);
    }
}
