using ElectronicService.Core.Catalog.Products.Analogies;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class CatalogProductAnalogyProposalEvaluatorTests
{
    private readonly Guid _manufacturerId = Guid.CreateVersion7();
    private readonly Guid _productTypeId = Guid.CreateVersion7();

    [Fact]
    public void EvaluateSuggestsMissingCharacteristicWithThreeConsistentProducts()
    {
        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.Evaluate(
                CreateRequest(),
                [
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B")
                ]));

        Assert.True(proposal.IsSuggested);
        Assert.Equal("CURVE", proposal.CharacteristicCode);
        Assert.Equal("B", proposal.ProposedValue);
        Assert.Equal(3, proposal.SupportingProductCount);
        Assert.Equal(1m, proposal.SupportRatio);
        Assert.Empty(proposal.BlockingReasons);
    }

    [Fact]
    public void EvaluateDoesNotSuggestKnownCharacteristic()
    {
        var request = CreateRequest(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CURVE"] = "B"
            });

        var proposals = CatalogProductAnalogyProposalEvaluator.Evaluate(
            request,
            [
                CreateMatch("CURVE", "B"),
                CreateMatch("CURVE", "B"),
                CreateMatch("CURVE", "B")
            ]);

        Assert.Empty(proposals);
    }

    [Fact]
    public void EvaluateBlocksProposalWithInsufficientSupport()
    {
        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.Evaluate(
                CreateRequest(),
                [CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B")]));

        Assert.False(proposal.IsSuggested);
        Assert.Contains(
            proposal.BlockingReasons,
            reason => reason.Contains("минимум 3", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateBlocksProposalWhenAnaloguesDisagree()
    {
        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.Evaluate(
                CreateRequest(),
                [
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "C")
                ]));

        Assert.False(proposal.IsSuggested);
        Assert.Equal(0.75m, proposal.SupportRatio);
        Assert.Equal(2, proposal.AlternativeValues.Count);
    }

    [Fact]
    public void EvaluateRejectsProductsConflictingWithKnownCharacteristics()
    {
        var proposals = CatalogProductAnalogyProposalEvaluator.Evaluate(
            CreateRequest(),
            [
                CreateMatch("CURVE", "B", conflictingCount: 1, conflictingCodes: ["SERIES"]),
                CreateMatch("CURVE", "B", conflictingCount: 1, conflictingCodes: ["SERIES"]),
                CreateMatch("CURVE", "B", conflictingCount: 1, conflictingCodes: ["SERIES"])
            ]);

        Assert.Empty(proposals);
    }

    [Fact]
    public void EvaluateKeepsSameFamilyProductsWithDifferentRatedCurrent()
    {
        var matches = Enumerable.Range(0, 3)
            .Select(_ => CreateMatch(
                "CURVE",
                "B",
                conflictingCount: 1,
                conflictingCodes: ["RATED_CURRENT"]))
            .ToArray();

        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.Evaluate(
                CreateRequest(),
                matches));

        Assert.True(proposal.IsSuggested);
        Assert.Equal("B", proposal.ProposedValue);
        Assert.Equal(3, proposal.SupportingProductCount);
    }

    [Fact]
    public void EvaluateProductTypesSuggestsTypeWhenThreeAnaloguesAgree()
    {
        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.EvaluateProductTypes(
                CreateRequest(includeProductType: false),
                [
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B"),
                    CreateMatch("CURVE", "B")
                ]));

        Assert.True(proposal.IsSuggested);
        Assert.Equal(_productTypeId, proposal.ProductTypeId);
        Assert.Equal(3, proposal.SupportingProductCount);
        Assert.Equal(1m, proposal.SupportRatio);
    }

    [Fact]
    public void EvaluateProductTypesReturnsNoProposalForAlreadySelectedType()
    {
        var proposals = CatalogProductAnalogyProposalEvaluator.EvaluateProductTypes(
            CreateRequest(),
            [
                CreateMatch("CURVE", "B"),
                CreateMatch("CURVE", "B"),
                CreateMatch("CURVE", "B")
            ]);

        Assert.Empty(proposals);
    }

    [Fact]
    public void EvaluateUsesCharacteristicThresholdsFromConfiguration()
    {
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            MinimumProductsWithCharacteristic = 2,
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                ["CURVE"] = new(CatalogProductAnalogyMode.Suggest, 2, 0.60m)
            }
        };

        var proposal = Assert.Single(
            CatalogProductAnalogyProposalEvaluator.Evaluate(
                CreateRequest(),
                [CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B")],
                settings));

        Assert.True(proposal.IsSuggested);
    }

    [Fact]
    public void EvaluateDoesNotReturnDisabledCharacteristic()
    {
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                ["CURVE"] = new(CatalogProductAnalogyMode.Off)
            }
        };

        var proposals = CatalogProductAnalogyProposalEvaluator.Evaluate(
            CreateRequest(),
            [CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B")],
            settings);

        Assert.Empty(proposals);
    }

    [Fact]
    public void EvaluateCalculatesShadowCharacteristicWithoutSuggestingIt()
    {
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                ["CURVE"] = new(CatalogProductAnalogyMode.Shadow)
            }
        };

        var proposal = Assert.Single(CatalogProductAnalogyProposalEvaluator.Evaluate(
            CreateRequest(),
            [CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B"), CreateMatch("CURVE", "B")],
            settings));

        Assert.False(proposal.IsSuggested);
        Assert.Equal(CatalogProductAnalogyMode.Shadow, proposal.Mode);
    }

    [Theory]
    [InlineData("RATED_CURRENT")]
    [InlineData("POLES")]
    [InlineData("LEAKAGE_CURRENT")]
    [InlineData("BREAKING_CAPACITY")]
    public void EvaluateNeverAllowsAutoFillForVariantCharacteristic(string code)
    {
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                [code] = new(CatalogProductAnalogyMode.AutoFill)
            }
        };

        var proposal = Assert.Single(CatalogProductAnalogyProposalEvaluator.Evaluate(
            CreateRequest(),
            [CreateMatch(code, "16"), CreateMatch(code, "16"), CreateMatch(code, "16")],
            settings));

        Assert.True(proposal.IsSuggested);
        Assert.False(proposal.IsAutoFillAllowed);
        Assert.NotNull(proposal.AutoFillBlockingReason);
    }

    [Fact]
    public void EvaluateAllowsAutoFillForStableCharacteristic()
    {
        var settings = CatalogProductAnalogySettings.SystemDefault with
        {
            Characteristics = new Dictionary<string, CatalogProductAnalogyCharacteristicSettings>(StringComparer.Ordinal)
            {
                ["SERIES"] = new(CatalogProductAnalogyMode.AutoFill)
            }
        };

        var proposal = Assert.Single(CatalogProductAnalogyProposalEvaluator.Evaluate(
            CreateRequest(),
            [CreateMatch("SERIES", "AV 10"), CreateMatch("SERIES", "AV 10"), CreateMatch("SERIES", "AV 10")],
            settings));

        Assert.True(proposal.IsAutoFillAllowed);
        Assert.Null(proposal.AutoFillBlockingReason);
    }

    private CatalogProductAnalogyRequest CreateRequest(
        IReadOnlyDictionary<string, string>? knownCharacteristics = null,
        bool includeProductType = true)
    {
        return new CatalogProductAnalogyRequest(
            "CHINT NB1 1P 16A",
            null,
            _manufacturerId,
            includeProductType ? _productTypeId : null,
            knownCharacteristics
                ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private CatalogProductAnalogyMatch CreateMatch(
        string characteristicCode,
        string characteristicValue,
        int conflictingCount = 0,
        IReadOnlyCollection<string>? conflictingCodes = null)
    {
        return new CatalogProductAnalogyMatch(
            Guid.CreateVersion7(),
            "NB1",
            "CHINT NB1 1P 16A",
            _manufacturerId,
            _productTypeId,
            "MODULAR_BREAKER",
            "Модульный автомат",
            "CHINT NB1 1P 16A",
            ExactArticleMatch: false,
            ExactNameMatch: false,
            SharedNameTokenCount: 4,
            QueryNameTokenCoverage: 1m,
            MatchingCharacteristicCount: 0,
            ConflictingCharacteristicCount: conflictingCount,
            SharedNameTokens: ["1P", "16A", "CHINT", "NB1"],
            MatchingCharacteristicCodes: [],
            ConflictingCharacteristicCodes: conflictingCodes
                ?? (conflictingCount > 0 ? ["POLES"] : []),
            Characteristics: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [characteristicCode] = characteristicValue
            },
            MatchedFamilyAnchor: "NB1",
            CandidateFamilyAnchors: ["NB1"]);
    }
}
