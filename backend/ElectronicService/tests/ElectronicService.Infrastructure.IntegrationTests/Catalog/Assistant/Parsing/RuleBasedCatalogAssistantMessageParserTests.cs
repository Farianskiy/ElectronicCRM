using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Assistant.Parsing;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog.Assistant.Parsing;

public sealed class RuleBasedCatalogAssistantMessageParserTests
{
    [Fact]
    public async Task ParseAsyncPrefersModelTokenOverGenericDictionarySearchToken()
    {
        var parser = CreateParser(
        [
            CreateTerm("автомат", "SearchToken", null, "АВТОМАТ")
        ]);

        var result = await parser.ParseAsync(
            "автомат NA1 2500M 3п стац 80кА",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal("NA1 2500M", result.Search);
    }

    [Fact]
    public async Task ParseAsyncDoesNotMatchCharacteristicInsideOppositeWord()
    {
        var parser = CreateParser(
        [
            CreateTerm(
                "реверс",
                "Characteristic",
                "CONTACTOR_EXECUTION",
                "РЕВЕРСИВНЫЙ")
        ]);

        var result = await parser.ParseAsync(
            "контактор нереверсивный",
            null,
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Characteristics);
    }

    [Fact]
    public async Task ParseAsyncSkipsUnresolvedWordAndSuggestsKnownMeaningForNextWord()
    {
        var resolver = new StubUnknownTermResolver(unknownPhrase =>
            string.Equals(unknownPhrase, "СТАЦ", StringComparison.Ordinal)
                ? new CatalogAssistantClarificationResult(
                    "СТАЦ",
                    "Characteristic",
                    "INSTALLATION_TYPE",
                    "СТАЦИОНАРНЫЙ",
                    0.85m,
                    "Возможно, вы имели в виду стационарное исполнение?",
                    true)
                : null);

        var parser = CreateParser([], resolver);

        var result = await parser.ParseAsync(
            "автомат стац",
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result.Clarification);
        Assert.Equal("СТАЦ", result.Clarification.UnknownPhrase);
        Assert.Equal(["АВТОМАТ", "СТАЦ"], resolver.ResolvedPhrases);
    }

    [Fact]
    public async Task ParseAsyncDoesNotUseElectricalMeasurementAsModelSearchToken()
    {
        var parser = CreateParser(
        [
            CreateTerm("контактор", "SearchToken", null, "КОНТАКТОР")
        ]);

        var result = await parser.ParseAsync(
            "контактор 115A 230V реверс",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal("КОНТАКТОР", result.Search);
    }

    private static RuleBasedCatalogAssistantMessageParser CreateParser(
        IReadOnlyCollection<CatalogDictionaryTermResult> terms,
        ICatalogAssistantUnknownTermResolver? unknownTermResolver = null)
    {
        return new RuleBasedCatalogAssistantMessageParser(
            new StubDictionaryReader(terms),
            unknownTermResolver ?? new StubUnknownTermResolver(_ => null),
            new EmptyRecognitionService(),
            new EmptyManufacturerResolver());
    }

    private static CatalogDictionaryTermResult CreateTerm(
        string phrase,
        string kind,
        string? targetCode,
        string targetValue)
    {
        return new CatalogDictionaryTermResult(
            Guid.NewGuid(),
            null,
            null,
            phrase,
            phrase.Trim().ToUpperInvariant().Replace("Ё", "Е", StringComparison.Ordinal),
            kind,
            targetCode,
            targetValue,
            100,
            "Approved",
            "Manual",
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

    private sealed class StubDictionaryReader(
        IReadOnlyCollection<CatalogDictionaryTermResult> terms)
        : ICatalogDictionaryReader
    {
        public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(terms);

        public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(terms);
    }

    private sealed class StubUnknownTermResolver(
        Func<string, CatalogAssistantClarificationResult?> resolve)
        : ICatalogAssistantUnknownTermResolver
    {
        public List<string> ResolvedPhrases { get; } = [];

        public Task<CatalogAssistantClarificationResult?> ResolveAsync(
            string unknownPhrase,
            CancellationToken cancellationToken = default)
        {
            ResolvedPhrases.Add(unknownPhrase);
            return Task.FromResult(resolve(unknownPhrase));
        }
    }

    private sealed class EmptyRecognitionService : ICatalogProductNameRecognitionService
    {
        public Task<CatalogProductNameRecognitionResult> RecognizeAsync(
            CatalogProductNameRecognitionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CatalogProductNameRecognitionResult(
                request.ProductName,
                request.ProductName.Trim().ToUpperInvariant(),
                [],
                [],
                []));
        }
    }

    private sealed class EmptyManufacturerResolver : IManufacturerResolver
    {
        public Task<ManufacturerResolutionIndex> LoadIndexAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ManufacturerResolutionIndex([], []));
        }
    }
}
