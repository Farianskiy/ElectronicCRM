using ElectronicService.Core.Catalog.Products.Analogies;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class CatalogProductAnalogyEvaluatorTests
{
    private readonly Guid _manufacturerId = Guid.CreateVersion7();
    private readonly Guid _productTypeId = Guid.CreateVersion7();

    [Fact]
    public void EvaluateRanksExactArticleBeforeNameSimilarity()
    {
        var request = CreateRequest(article: "NB1-63-C16");

        var exactArticle = CreateCandidate(
            article: "NB1 63 C16",
            name: "Другой текст товара");

        var similarName = CreateCandidate(
            article: "OTHER",
            name: request.ProductName);

        var result = CatalogProductAnalogyEvaluator.Evaluate(
            request,
            [similarName, exactArticle]);

        Assert.Equal(exactArticle.ProductId, result.Matches.First().ProductId);
        Assert.True(result.Matches.First().ExactArticleMatch);
    }

    [Fact]
    public void EvaluateUsesAliasAsNameEvidence()
    {
        var request = CreateRequest();
        var candidate = CreateCandidate(
            name: "Старое наименование",
            aliases: [request.ProductName]);

        var match = Assert.Single(
            CatalogProductAnalogyEvaluator.Evaluate(request, [candidate]).Matches);

        Assert.True(match.ExactNameMatch);
        Assert.Equal(request.ProductName, match.MatchedName);
    }

    [Fact]
    public void EvaluatePrefersCandidateWithoutCharacteristicConflict()
    {
        var request = CreateRequest(
            knownCharacteristics: new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["POLES"] = "1P",
                ["RATED_CURRENT"] = "16"
            });

        var conflicting = CreateCandidate(
            article: "CONFLICT",
            characteristics: new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["POLES"] = "3P",
                ["RATED_CURRENT"] = "16"
            });

        var consistent = CreateCandidate(
            article: "CONSISTENT",
            characteristics: new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["POLES"] = "1P",
                ["RATED_CURRENT"] = "16"
            });

        var matches = CatalogProductAnalogyEvaluator
            .Evaluate(request, [conflicting, consistent])
            .Matches
            .ToArray();

        Assert.Equal(consistent.ProductId, matches[0].ProductId);
        Assert.Equal(2, matches[0].MatchingCharacteristicCount);
        Assert.Equal(0, matches[0].ConflictingCharacteristicCount);
        Assert.Equal(1, matches[1].ConflictingCharacteristicCount);
    }

    [Fact]
    public void EvaluateExcludesWrongScopeArchivedAndCurrentProducts()
    {
        var excludedProductId = Guid.CreateVersion7();
        var request = CreateRequest(excludedProductId: excludedProductId);

        var current = CreateCandidate(productId: excludedProductId);
        var archived = CreateCandidate(isArchived: true);
        var wrongManufacturer = CreateCandidate(
            manufacturerId: Guid.CreateVersion7());
        var wrongType = CreateCandidate(
            productTypeId: Guid.CreateVersion7());
        var eligible = CreateCandidate();

        var result = CatalogProductAnalogyEvaluator.Evaluate(
            request,
            [current, archived, wrongManufacturer, wrongType, eligible]);

        var match = Assert.Single(result.Matches);
        Assert.Equal(eligible.ProductId, match.ProductId);
        Assert.Equal(1, result.EvaluatedCandidatesCount);
        Assert.Equal(4, result.ExcludedCandidatesCount);
        Assert.Contains(
            result.ExcludedCandidates ?? [],
            item => string.Equals(
                item.ReasonCode,
                "archived",
                StringComparison.Ordinal));
        Assert.Contains(
            result.ExcludedCandidates ?? [],
            item => string.Equals(
                item.ReasonCode,
                "current_product",
                StringComparison.Ordinal));
        Assert.Contains(
            result.ExcludedCandidates ?? [],
            item => string.Equals(
                item.ReasonCode,
                "manufacturer_mismatch",
                StringComparison.Ordinal));
        Assert.Contains(
            result.ExcludedCandidates ?? [],
            item => string.Equals(
                item.ReasonCode,
                "product_type_mismatch",
                StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateRequiresMeaningfulEvidence()
    {
        var request = CreateRequest();
        var candidate = CreateCandidate(
            article: "OTHER",
            name: "CHINT совершенно другой товар");

        var result = CatalogProductAnalogyEvaluator.Evaluate(request, [candidate]);

        Assert.Empty(result.Matches);
        var exclusion = Assert.Single(result.ExcludedCandidates ?? []);
        Assert.Equal("family_evidence_missing", exclusion.ReasonCode);
        Assert.Contains("семейство", exclusion.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Авт. выкл. AV 10 1P 13A", "AV 10 3P 25A", "AV10")]
    [InlineData("ПРК32 10 Ir=6-10A", "ПРК32 16 Ir=10-16A", "ПРК32")]
    [InlineData("АВДТ32 C16 30mA", "АВДТ32 C25 30mA", "АВДТ32")]
    [InlineData("ВД1-63 2P 25A", "ВД1-63 4P 40A", "ВД163")]
    public void EvaluateUsesGenericFamilyAnchor(
        string requestName,
        string candidateName,
        string expectedAnchor)
    {
        var request = new CatalogProductAnalogyRequest(
            requestName,
            null,
            _manufacturerId,
            _productTypeId,
            new Dictionary<string, string>(StringComparer.Ordinal));
        var candidate = CreateCandidate(article: "", name: candidateName);

        var match = Assert.Single(
            CatalogProductAnalogyEvaluator.Evaluate(request, [candidate]).Matches);

        Assert.Equal(expectedAnchor, match.MatchedFamilyAnchor);
    }

    [Fact]
    public void EvaluateDoesNotTreatGenericWordsAndNominalsAsFamily()
    {
        var request = new CatalogProductAnalogyRequest(
            "Автоматический выключатель 1P 16A",
            null,
            _manufacturerId,
            _productTypeId,
            new Dictionary<string, string>(StringComparer.Ordinal));
        var candidate = CreateCandidate(
            article: "",
            name: "Автоматический выключатель 1P 25A");

        Assert.Empty(
            CatalogProductAnalogyEvaluator.Evaluate(request, [candidate]).Matches);
    }

    private CatalogProductAnalogyRequest CreateRequest(
        string? article = null,
        IReadOnlyDictionary<string, string>? knownCharacteristics = null,
        Guid? excludedProductId = null)
    {
        return new CatalogProductAnalogyRequest(
            "CHINT NB1-63 1P 16A C",
            article,
            _manufacturerId,
            _productTypeId,
            knownCharacteristics
                ?? new Dictionary<string, string>(StringComparer.Ordinal),
            excludedProductId);
    }

    private CatalogProductAnalogyCandidate CreateCandidate(
        Guid? productId = null,
        string article = "NB1-63",
        string name = "CHINT NB1-63 1P 10A B",
        Guid? manufacturerId = null,
        Guid? productTypeId = null,
        IReadOnlyCollection<string>? aliases = null,
        IReadOnlyDictionary<string, string>? characteristics = null,
        bool isArchived = false)
    {
        return new CatalogProductAnalogyCandidate(
            productId ?? Guid.CreateVersion7(),
            article,
            name,
            manufacturerId ?? _manufacturerId,
            productTypeId ?? _productTypeId,
            "MODULAR_BREAKER",
            "Модульный автомат",
            aliases ?? [],
            characteristics
                ?? new Dictionary<string, string>(StringComparer.Ordinal),
            isArchived);
    }
}
