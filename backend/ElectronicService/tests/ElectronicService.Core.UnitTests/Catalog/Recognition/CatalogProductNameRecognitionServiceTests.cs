using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Recognition;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.UnitTests.Catalog.Recognition;

public sealed class CatalogProductNameRecognitionServiceTests
{
    [Fact]
    public async Task RecognizeAsyncRecognizesApprovedProductSeriesDictionaryTerm()
    {
        var reader = new FakeCatalogDictionaryReader(
            CreateProductSeriesTerm("NB1", "NB1"));
        var service = CreateService(reader);

        var result = await service.RecognizeAsync(
            CreateRequest("NB1 63 1п 10А B 6кА CHINT"),
            TestContext.Current.CancellationToken);

        var characteristic = Assert.Single(result.Characteristics);

        Assert.Equal("PRODUCT_SERIES", characteristic.CharacteristicCode);
        Assert.Equal("NB1", characteristic.NormalizedValue);
        Assert.Equal(CatalogRecognitionSource.Dictionary, characteristic.Source);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task RecognizeAsyncUsesLongestMatchingDictionaryTerm()
    {
        var reader = new FakeCatalogDictionaryReader(
            CreateProductSeriesTerm("NB1", "NB1"),
            CreateProductSeriesTerm("NB1 63", "NB1-63"));
        var service = CreateService(reader);

        var result = await service.RecognizeAsync(
            CreateRequest("NB1 63 1п 10А B 6кА CHINT"),
            TestContext.Current.CancellationToken);

        var characteristic = Assert.Single(result.Characteristics);

        Assert.Equal("NB1-63", characteristic.NormalizedValue);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task RecognizeAsyncDoesNotMatchDictionaryTermInsideAnotherToken()
    {
        var reader = new FakeCatalogDictionaryReader(
            CreateProductSeriesTerm("NB1", "NB1"));
        var service = CreateService(reader);

        var result = await service.RecognizeAsync(
            CreateRequest("NB10 63 1п 10А B 6кА CHINT"),
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Characteristics);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task RecognizeAsyncLoadsApprovedDictionaryTermsOncePerServiceScope()
    {
        var reader = new FakeCatalogDictionaryReader(
            CreateProductSeriesTerm("NB1", "NB1"));
        var service = CreateService(reader);

        await service.RecognizeAsync(
            CreateRequest("NB1 first"),
            TestContext.Current.CancellationToken);
        await service.RecognizeAsync(
            CreateRequest("NB1 second"),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, reader.ApprovedTermsReadCount);
    }

    private static CatalogProductNameRecognitionService CreateService(
        ICatalogDictionaryReader reader)
    {
        return new CatalogProductNameRecognitionService(
            Array.Empty<ICatalogCharacteristicRecognitionStrategy>(),
            reader,
            new EmptyRecognitionProfileReader());
    }

    private static CatalogProductNameRecognitionRequest CreateRequest(string productName)
    {
        return new CatalogProductNameRecognitionRequest(
            productName,
            Guid.NewGuid(),
            ["PRODUCT_SERIES"]);
    }

    private static CatalogDictionaryTermResult CreateProductSeriesTerm(
        string phrase,
        string targetValue)
    {
        return new CatalogDictionaryTermResult(
            Guid.NewGuid(),
            null,
            null,
            phrase,
            phrase.ToUpperInvariant(),
            "Characteristic",
            "PRODUCT_SERIES",
            targetValue,
            100,
            "Approved",
            "Seed",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private sealed class EmptyRecognitionProfileReader
        : ICatalogCharacteristicRecognitionProfileReader
    {
        public Task<IReadOnlyCollection<
            CatalogCharacteristicRecognitionProfileResult>> GetProfilesAsync(
                Guid productTypeId,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyCollection<
                CatalogCharacteristicRecognitionProfileResult>>([]);
        }
    }

    private sealed class FakeCatalogDictionaryReader : ICatalogDictionaryReader
    {
        private readonly IReadOnlyCollection<CatalogDictionaryTermResult> _approvedTerms;

        public FakeCatalogDictionaryReader(
            params CatalogDictionaryTermResult[] approvedTerms)
        {
            _approvedTerms = approvedTerms;
        }

        public int ApprovedTermsReadCount { get; private set; }

        public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_approvedTerms);
        }

        public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApprovedTermsReadCount++;

            return Task.FromResult(_approvedTerms);
        }
    }
}
