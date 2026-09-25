using System.Text.Json;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Core.Catalog.Recognition.Effective;
using ElectronicService.Core.Catalog.Recognition.Preview;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.CatalogImport.UnitTests;

public sealed class CatalogEffectiveRecognitionTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("learned", true, false)]
    [InlineData("C learned", true, true)]
    [InlineData("C", false, false)]
    public async Task PreviewAndImportUseIdenticalCandidatesValuesAndConflicts(string name, bool active, bool conflict)
    {
        var catalog = new RecognitionTestCatalog();
        var version = active ? catalog.Activate() : null;
        var preview = await new PreviewCatalogProductNameRecognitionQueryHandler(catalog, catalog, catalog.Service())
            .Handle(new(name, catalog.ProductType.Code, catalog.Manufacturer.Id), TestContext.Current.CancellationToken);
        var imported = await EnrichAsync(catalog, catalog.Service(), name);
        Assert.True(preview.IsSuccess);
        var recognized = Assert.Single(imported.RecognitionResults!).Value.Result;
        Assert.Equal(JsonSerializer.Serialize(preview.Value.RecognitionResult), JsonSerializer.Serialize(recognized.Recognition));
        Assert.Equal(conflict, recognized.Recognition.Conflicts.Count > 0);
        Assert.Equal(version?.VersionId, preview.Value.ActiveRuleSetVersionId);
        Assert.Equal(version?.VersionId, recognized.ActiveRuleSet?.ActiveVersionId);
        Assert.True(preview.Value.HasCompleteScope);
        if (active && !conflict)
        {
            Assert.Contains("active-rule-set:", Assert.Single(recognized.Recognition.Characteristics).RecognizerKey, StringComparison.Ordinal);
            Assert.Equal("D", Data(Assert.Single(imported.Analysis.Rows)).Characteristics[catalog.Definition.Id.ToString()]);
        }

        var shadow = new CatalogImportRecognitionShadowService().Analyze(imported.Analysis, imported.RecognitionResults!, TestContext.Current.CancellationToken);
        Assert.Equal(conflict ? 1 : 0, shadow.AmbiguousCount);
        Assert.Equal(recognized.Recognition.Candidates.Count, Assert.Single(shadow.EvidenceRows).Evidence.Count);
        var explanation = Explain(imported.Analysis, shadow);
        Assert.Equal(1, explanation.RowsWithEvidenceCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingManufacturerPreservesLegacyPreviewAndReportsIncompleteScope(bool withType)
    {
        var catalog = new RecognitionTestCatalog();
        catalog.Activate();
        var result = await new PreviewCatalogProductNameRecognitionQueryHandler(catalog, catalog, catalog.Service())
            .Handle(new("C learned", withType ? catalog.ProductType.Code : null), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasCompleteScope);
        Assert.Null(result.Value.ManufacturerId);
        Assert.Null(result.Value.ActiveRuleSetVersionId);
        Assert.Equal("C", Assert.Single(result.Value.RecognitionResult.Characteristics).NormalizedValue);
        Assert.Equal(0, catalog.VersionReads);
    }

    [Fact]
    public async Task ActiveVersionLoadFailureIsVisibleInPreviewAndImport()
    {
        var catalog = new RecognitionTestCatalog();
        catalog.Activate();
        catalog.LoadError = new DomainError("training.invalid_data", "Broken active version");
        var preview = await new PreviewCatalogProductNameRecognitionQueryHandler(catalog, catalog, catalog.Service())
            .Handle(new("C learned", catalog.ProductType.Code, catalog.Manufacturer.Id), TestContext.Current.CancellationToken);
        var service = new CatalogImportRecognitionEnrichmentService(catalog.Service(), new CatalogImportRowValidator());
        var import = await service.EnrichAsync(Analysis(Row(catalog, "C learned")), catalog.ProductType, [catalog.Definition], TestContext.Current.CancellationToken);
        Assert.True(preview.IsFailure);
        Assert.True(import.IsFailure);
        Assert.Equal("recognition.active_rule_invalid", preview.Error.Code);
        Assert.Equal(preview.Error, import.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidExecutionOrWrongScopeNeverFallsBackToBaseline(bool wrongScope)
    {
        var catalog = new RecognitionTestCatalog();
        var version = catalog.Activate();
        catalog.Versions[version.VersionId] = wrongScope
            ? version with { ProductTypeId = catalog.OtherType.Id }
            : version with { LiteralRules = [version.LiteralRules[0] with { GeneratorVersion = "unsupported" }] };
        var result = await new PreviewCatalogProductNameRecognitionQueryHandler(catalog, catalog, catalog.Service())
            .Handle(new("C learned", catalog.ProductType.Code, catalog.Manufacturer.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.Equal(wrongScope ? "recognition.scope_mismatch" : "recognition.active_rule_invalid", result.Error.Code);
    }

    [Fact]
    public async Task InvalidExplicitExcelValueIsNotReplacedByActiveRule()
    {
        var catalog = new RecognitionTestCatalog();
        catalog.Activate();
        var row = Row(catalog, "learned");
        var issue = new CatalogImportRowIssue("characteristic.invalid", "Invalid Excel value", catalog.Definition.Id.ToString(), null);
        Assert.True(row.ReplaceValidationResult(CatalogImportRowStatus.Error, row.NormalizedDataJson,
            JsonSerializer.Serialize(new[] { issue }, JsonOptions), "[]").IsSuccess);
        var result = await new CatalogImportRecognitionEnrichmentService(catalog.Service(), new CatalogImportRowValidator())
            .EnrichAsync(Analysis(row), catalog.ProductType, [catalog.Definition], TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Empty(Data(row).Characteristics);
        Assert.Equal(1, result.Value.Summary.BlockedByInvalidExcelValueCount);
        Assert.Contains("characteristic.invalid", row.IssuesJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunPinsAllActiveScopesAndCachesProfilesAndDictionaryUntilNextRun()
    {
        var catalog = new RecognitionTestCatalog();
        var firstVersion = catalog.Activate();
        var otherVersion = catalog.Activate("B", catalog.OtherType);
        catalog.Profiles = [catalog.Profile(true)];
        catalog.Terms = [catalog.Term("C")];
        var service = catalog.Service();
        var run = await service.CreateRunAsync(TestContext.Current.CancellationToken);
        var first = await service.RecognizeAsync(new("C learned", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id, [catalog.Definition], run), TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);
        Assert.Single(first.Value.Recognition.Conflicts);
        var newVersion = catalog.Activate("B");
        catalog.Activate("D", catalog.OtherType);
        catalog.Profiles = [catalog.Profile(false)];
        catalog.Terms = [catalog.Term("B")];
        var again = await service.RecognizeAsync(new("C learned", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id, [catalog.Definition], run), TestContext.Current.CancellationToken);
        var dictionary = await service.RecognizeAsync(new("dictionary", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id, [catalog.Definition], run), TestContext.Current.CancellationToken);
        var other = await service.RecognizeAsync(new("learned", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.OtherType.Id, [catalog.Definition], run), TestContext.Current.CancellationToken);
        Assert.Equal(firstVersion.VersionId, again.Value.ActiveRuleSet?.ActiveVersionId);
        Assert.Single(again.Value.Recognition.Conflicts);
        Assert.Equal("C", Assert.Single(dictionary.Value.Recognition.Characteristics).NormalizedValue);
        Assert.Equal(otherVersion.VersionId, other.Value.ActiveRuleSet?.ActiveVersionId);
        Assert.Equal(1, catalog.ProfileReads[catalog.ProductType.Id]);
        Assert.Equal(1, catalog.DictionaryReads);
        var nextRun = await service.CreateRunAsync(TestContext.Current.CancellationToken);
        var next = await service.RecognizeAsync(new("C learned", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id, [catalog.Definition], nextRun), TestContext.Current.CancellationToken);
        var nextDictionary = await service.RecognizeAsync(new("dictionary", catalog.Manufacturer.Id, catalog.Manufacturer.Name, catalog.ProductType.Id, [catalog.Definition], nextRun), TestContext.Current.CancellationToken);
        Assert.Equal(newVersion.VersionId, next.Value.ActiveRuleSet?.ActiveVersionId);
        Assert.Empty(next.Value.Recognition.Conflicts);
        Assert.Equal("B", Assert.Single(next.Value.Recognition.Characteristics).NormalizedValue);
        Assert.Equal("B", Assert.Single(nextDictionary.Value.Recognition.Characteristics).NormalizedValue);
        Assert.Equal(2, catalog.DictionaryReads);
        Assert.Equal(2, catalog.ProfileReads[catalog.ProductType.Id]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitValuesAndManualCorrectionsSurviveWhileExplanationUsesEffectiveRecognition(bool manual)
    {
        var catalog = new RecognitionTestCatalog();
        catalog.Activate();
        var imported = await EnrichAsync(catalog, catalog.Service(), "learned", "B", manual);
        var row = Assert.Single(imported.Analysis.Rows);
        Assert.Equal("B", Data(row).Characteristics[catalog.Definition.Id.ToString()]);
        Assert.Equal(manual, !row.IssuesJson.Contains("characteristic.value_conflict", StringComparison.Ordinal));
        var shadow = new CatalogImportRecognitionShadowService().Analyze(imported.Analysis, imported.RecognitionResults!, TestContext.Current.CancellationToken);
        Assert.Equal("D", Assert.Single(Assert.Single(shadow.EvidenceRows).Evidence).TargetValue);
        Assert.Equal("B", Assert.Single(shadow.ConflictGroups).ExcelValue);
        Assert.Equal(1, Explain(imported.Analysis, shadow).RowsWithEvidenceCount);
    }

    [Fact]
    public async Task MixedTypesReuseRunAndExplainBothScopesWithoutAnotherRecognitionPass()
    {
        var catalog = new RecognitionTestCatalog();
        var first = catalog.Activate();
        var second = catalog.Activate("B", catalog.OtherType);
        var service = new CatalogImportRecognitionEnrichmentService(catalog.Service(), new CatalogImportRowValidator());
        await service.PrepareRunAsync(TestContext.Current.CancellationToken);
        var firstResult = await service.EnrichAsync(Analysis(Row(catalog, "learned")), catalog.ProductType, [catalog.Definition], TestContext.Current.CancellationToken);
        catalog.Activate("C", catalog.OtherType);
        var otherRow = Row(catalog, "learned", typeId: catalog.OtherType.Id, number: 3);
        var otherResult = await service.EnrichAsync(Analysis(otherRow), catalog.OtherType, [catalog.Definition], TestContext.Current.CancellationToken);
        Assert.True(firstResult.IsSuccess);
        Assert.True(otherResult.IsSuccess);
        var results = firstResult.Value.RecognitionResults!.Concat(otherResult.Value.RecognitionResults!).ToDictionary();
        Assert.Equal(first.VersionId, results[2].Result.ActiveRuleSet?.ActiveVersionId);
        Assert.Equal(second.VersionId, results[3].Result.ActiveRuleSet?.ActiveVersionId);
        var analysis = Analysis(firstResult.Value.Analysis.Rows.Concat(otherResult.Value.Analysis.Rows).ToArray());
        var shadow = new CatalogImportRecognitionShadowService().Analyze(analysis, results, TestContext.Current.CancellationToken);
        Assert.Equal(2, shadow.RowsWithRecognition);
        Assert.Equal(2, Explain(analysis, shadow).RowsWithEvidenceCount);
        Assert.Equal(1, catalog.Captures);
        Assert.Equal(2, catalog.VersionReads);
    }

    private static async Task<CatalogImportRecognitionEnrichmentResult> EnrichAsync(RecognitionTestCatalog catalog,
        ICatalogEffectiveRecognitionService effective, string name, string? explicitValue = null, bool manual = false)
    {
        var result = await new CatalogImportRecognitionEnrichmentService(effective, new CatalogImportRowValidator())
            .EnrichAsync(Analysis(Row(catalog, name, explicitValue, manual)), catalog.ProductType, [catalog.Definition], TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    private static CatalogImportRow Row(RecognitionTestCatalog catalog, string name, string? value = null, bool manual = false, Guid? typeId = null, int number = 2)
    {
        var key = catalog.Definition.Id.ToString();
        var data = new CatalogImportNormalizedRowData(name, "article", catalog.Manufacturer.Name, null, null,
            value is null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal) { [key] = value },
            catalog.Manufacturer.Id, CharacteristicOrigins: manual ? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal) { [key] = CatalogImportCharacteristicValueOrigin.FromManual() } : null,
            ProductTypeId: typeId ?? catalog.ProductType.Id);
        return CatalogImportRow.Create(Guid.NewGuid(), number, CatalogImportRowStatus.Valid, "{}", JsonSerializer.Serialize(data, JsonOptions), "[]", "[]").Value;
    }

    private static CatalogImportNormalizedRowData Data(CatalogImportRow row)
        => JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions)!;

    private static CatalogImportWorkbookAnalysis Analysis(params CatalogImportRow[] rows)
        => new([], rows, false, rows.Length, 0, new(0, 0, 0, 0, 0, []));

    private static CatalogImportProductNameExplanationSummary Explain(CatalogImportWorkbookAnalysis analysis, CatalogImportRecognitionShadowResult shadow)
        => new CatalogImportProductNameExplanationService(new CatalogProductNameEvidenceCoverageService()).Analyze(analysis,
            new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, [], []),
            new(0, false, null, null, null, 0, 0, 0, 0, 0, 0, 0, false, false, [], [], []), shadow, TestContext.Current.CancellationToken);
}
