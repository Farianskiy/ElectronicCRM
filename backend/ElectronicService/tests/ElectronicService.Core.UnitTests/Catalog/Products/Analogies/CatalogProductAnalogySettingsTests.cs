using ElectronicService.Core.Catalog.Products.Analogies;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class CatalogProductAnalogySettingsTests
{
    [Fact]
    public void NormalizeProducesSameHashForDifferentCharacteristicOrder()
    {
        var first = CatalogProductAnalogySettings.SystemDefault with
        {
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                ["curve"] = new(CatalogProductAnalogyMode.Shadow),
                ["poles"] = new(CatalogProductAnalogyMode.Off)
            }
        };
        var second = first with
        {
            Characteristics = first.Characteristics.Reverse()
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal)
        };

        var firstResult = CatalogProductAnalogySettingsCodec.Normalize(first);
        var secondResult = CatalogProductAnalogySettingsCodec.Normalize(second);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(firstResult.Value.Hash, secondResult.Value.Hash);
        Assert.Contains("CURVE", firstResult.Value.Settings.Characteristics.Keys);
    }

    [Fact]
    public void NormalizeRejectsUnsafeCandidatePoolSize()
    {
        var result = CatalogProductAnalogySettingsCodec.Normalize(
            CatalogProductAnalogySettings.SystemDefault with
            {
                MaximumCandidatePoolSize = 10001
            });

        Assert.True(result.IsFailure);
    }
}
