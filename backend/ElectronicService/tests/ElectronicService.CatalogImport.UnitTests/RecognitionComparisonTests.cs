using System.Text.Json;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Core.Catalog.Recognition.Training;

namespace ElectronicService.CatalogImport.UnitTests;

public sealed class RecognitionComparisonTests
{
    [Fact]
    public async Task SnapshotReplayUsesSharedRecognizerWithoutLiveReadsAndDeduplicatesLabels()
    {
        var catalog = new RecognitionTestCatalog();
        var input = Input(catalog);
        input = input with { Examples = [input.Examples[0], input.Examples[0] with { Id = Guid.NewGuid(), FeedbackId = Guid.NewGuid() }] };
        var evaluator = new RecognitionComparisonEvaluator(catalog.Service());
        var first = await evaluator.EvaluateAsync(input, TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.DuplicateExamples);
        Assert.Equal(1, first.Value.Control.Current.Total);
        Assert.Equal(1, first.Value.Control.Current.Missing);
        Assert.Equal(1, first.Value.Control.Candidate.Correct);
        Assert.Equal("Ready", first.Value.Readiness.State);
        var restored = JsonSerializer.Deserialize<EvaluationInput>(EvaluationJson.Serialize(input), EvaluationJson.Options)!;
        catalog.LoadError = new("test.no_live_read", "Must not read live rules");
        catalog.Terms = [catalog.Term("C")];
        var repeated = await evaluator.EvaluateAsync(restored, TestContext.Current.CancellationToken);
        Assert.Equal(EvaluationJson.Serialize(first.Value), EvaluationJson.Serialize(repeated.Value));
        Assert.Equal(0, catalog.VersionReads);
        Assert.Equal(0, catalog.DictionaryReads);
        Assert.Empty(catalog.ProfileReads);
    }

    [Fact]
    public async Task AllCharacteristicsOfOverlappingNameAreExcludedAndZeroDenominatorsAreNull()
    {
        var catalog = new RecognitionTestCatalog();
        var input = Input(catalog) with { EvidenceNames = [CatalogRecognitionTextNormalizer.NormalizeText("learned device")] };
        var result = (await new RecognitionComparisonEvaluator(catalog.Service()).EvaluateAsync(input, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(0, result.Control.Current.Total);
        Assert.Null(result.Control.Current.CorrectRate);
        Assert.Equal(1, result.TrainingDiagnostic.Candidate.Correct);
        Assert.Equal("InsufficientData", result.Readiness.State);
    }

    [Fact]
    public async Task ContradictoryLabelsBlockReleaseInsteadOfMajorityVoting()
    {
        var catalog = new RecognitionTestCatalog();
        var input = Input(catalog);
        input = input with { Examples = [input.Examples[0], input.Examples[0] with { Id = Guid.NewGuid(), Value = "C" }] };
        var result = (await new RecognitionComparisonEvaluator(catalog.Service()).EvaluateAsync(input, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(1, result.LabelConflicts);
        Assert.Equal(0, result.Control.Current.Total);
        Assert.Contains(result.Readiness.Reasons, x => string.Equals(x.Code, "evaluation.label_conflict", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("C", false, "Incorrect")]
    [InlineData("D", true, "Conflict")]
    public async Task RegressionAndNewConflictsBlockRelease(string candidateValue, bool conflict, string expectedStatus)
    {
        var catalog = new RecognitionTestCatalog();
        var input = Input(catalog);
        var previous = input.CandidateRules!;
        var proposed = previous with { VersionId = Guid.NewGuid(), LiteralRules = [previous.LiteralRules[0] with { NormalizedValue = candidateValue }] };
        if (conflict) proposed = proposed with { LiteralRules = [.. proposed.LiteralRules, proposed.LiteralRules[0] with { DraftId = Guid.NewGuid(), NormalizedValue = "C" }] };
        input = input with { CurrentState = input.CurrentState with { ActiveVersionId = previous.VersionId }, CurrentRules = previous, CandidateRules = proposed };
        var result = (await new RecognitionComparisonEvaluator(catalog.Service()).EvaluateAsync(input, TestContext.Current.CancellationToken)).Value;
        Assert.Equal(1, result.Control.Regressions);
        Assert.Equal(expectedStatus, Assert.Single(result.Rows).Candidate.Status);
        Assert.Contains(result.Readiness.Reasons, x => string.Equals(x.Code, "evaluation.regression", StringComparison.Ordinal));
    }

    [Fact]
    public void CanonicalJsonIgnoresPropertyOrderAndNumericFormatting()
    {
        Assert.Equal(EvaluationJson.Canonicalize("{\"b\":1.00,\"a\":{\"x\":2}}"), EvaluationJson.Canonicalize("{\"a\":{\"x\":2.0},\"b\":1}"));
        Assert.False(new RecognitionEvaluationOptions { MinimumControlNames = 0 }.IsValid());
    }

    [Fact]
    public async Task DictionaryComparisonPreservesActiveRulesAndDetectsConflictWithoutLiveReads()
    {
        var catalog = new RecognitionTestCatalog();
        var input = Input(catalog);
        input = input with { CurrentState = input.CurrentState with { ActiveVersionId = input.CandidateRules!.VersionId },
            CurrentRules = input.CandidateRules, CandidateTerms = [catalog.Term("C")],
            Examples = input.Examples.Select(x => x with { ProductName = "learned dictionary" }).ToArray() };
        var evaluated = await new RecognitionComparisonEvaluator(catalog.Service()).EvaluateAsync(input, TestContext.Current.CancellationToken);
        Assert.True(evaluated.IsSuccess);
        Assert.Equal(1, evaluated.Value.Control.Current.Correct);
        Assert.Equal(1, evaluated.Value.Control.Candidate.Conflicts);
        Assert.Equal(1, evaluated.Value.Control.Regressions);
        Assert.Equal(0, catalog.VersionReads);
        Assert.Equal(0, catalog.DictionaryReads);
    }

    [Fact]
    public void ExistingRuleSnapshotEncodingDoesNotIncludeDictionaryExtension()
    {
        var input = Input(new RecognitionTestCatalog());
        var json = EvaluationJson.Serialize(input);
        Assert.DoesNotContain("candidateTerms", json, StringComparison.Ordinal);
        var restored = JsonSerializer.Deserialize<EvaluationInput>(json, EvaluationJson.Options)!;
        Assert.Equal(EvaluationJson.Fingerprint(input), EvaluationJson.Fingerprint(restored));
        Assert.Null(restored.CandidateTerms);
    }

    private static EvaluationInput Input(RecognitionTestCatalog catalog)
    {
        var rules = catalog.Activate("D");
        return new(catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id,
            new(catalog.Manufacturer.Id, catalog.ProductType.Id, 0, null, null, null, null), null, rules,
            [new(catalog.Definition.Id, catalog.Definition.Code, catalog.Definition.Name, catalog.Definition.DataType, catalog.Definition.Unit)], [], [],
            [new(Guid.NewGuid(), Guid.NewGuid(), "learned device", catalog.Definition.Id, "D", true)], [], new("release-policy-v1", 1, 1, 1));
    }
}
