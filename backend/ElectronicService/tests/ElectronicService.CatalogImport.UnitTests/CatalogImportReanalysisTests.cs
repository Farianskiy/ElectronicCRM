using System.Text.Json;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.TestCommon;

namespace ElectronicService.CatalogImport.UnitTests;

public sealed class CatalogImportReanalysisTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("{\"1\":\"179635\",\"2\":\"Автомат\"}", "{ \"2\": \"Автомат\", \"1\": \"179635\" }")]
    [InlineData("{\"1\":\"А\"}", "{\"1\":\"\\u0410\"}")]
    public void ReanalysisMatchesJsonContentAndRetainsSavedIdentity(string saved, string generated)
    {
        var batchId = Guid.NewGuid();
        var oldRow = CreateRow(batchId, saved);
        var newRow = CreateRow(batchId, generated);
        var result = CatalogImportReanalysisRows.Prepare([newRow], [oldRow]);
        Assert.True(result.IsSuccess);
        Assert.Same(oldRow, Assert.Single(result.Value));
    }

    [Theory]
    [InlineData("{\"1\":\"a\"}", "{\"1\":\"b\"}")]
    [InlineData("{\"1\":\"a\"}", "{\"2\":\"a\"}")]
    [InlineData("{\"1\":\"a\"}", "{\"1\":\" a\"}")]
    public void ReanalysisRejectsChangedSource(string saved, string generated)
    {
        var batchId = Guid.NewGuid();
        var result = CatalogImportReanalysisRows.Prepare(
            [CreateRow(batchId, generated)], [CreateRow(batchId, saved)]);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ReanalysisRejectsMissingOrAmbiguousSourceRow()
    {
        var batchId = Guid.NewGuid();
        var saved = CreateRow(batchId, "{}");
        Assert.True(CatalogImportReanalysisRows.Prepare([], [saved]).IsFailure);
        Assert.True(CatalogImportReanalysisRows.Prepare(
            [CreateRow(batchId, "{}"), CreateRow(batchId, "{}")], [saved]).IsFailure);
        Assert.True(CatalogImportReanalysisRows.Prepare(
            [CreateRow(Guid.NewGuid(), "{}")], [saved]).IsFailure);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("C")]
    [InlineData("")]
    public async Task SavedScopeUsesActiveRulesAndPreservesManualCharacteristic(string? manualValue)
    {
        var manufacturerId = Guid.NewGuid();
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            "TRIP_CURVE", "Характеристика срабатывания", CharacteristicDataType.Text, null);
        TestDataFactory.AddCharacteristic(productType, definition, isRequired: true);
        var key = definition.Id.ToString();
        var characteristics = new Dictionary<string, string>(StringComparer.Ordinal);
        var origins = new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal);
        if (manualValue is not null)
        {
            characteristics[key] = manualValue;
            origins[key] = CatalogImportCharacteristicValueOrigin.FromManual();
        }

        var data = new CatalogImportNormalizedRowData(
            "Авт. выкл. NB1-63 1P 32А 6кА х-ка D (DB) (R)", "179635", "CHINT", 123m, 7,
            characteristics, manufacturerId, "Manual", CharacteristicOrigins: origins,
            ProductTypeId: productType.Id, ProductTypeResolutionSource: "Manual");
        var saved = CreateRow(Guid.NewGuid(), "{}", data);
        var prepared = CatalogImportReanalysisRows.Prepare([CreateRow(saved.BatchId, "{}")], [saved]);
        Assert.True(prepared.IsSuccess);
        var analysis = new CatalogImportWorkbookAnalysis([], prepared.Value, false, 0, 1,
            new CatalogImportManufacturerResolutionSummary(0, 0, 0, 0, 0, []));

        var assignment = await new CatalogImportProductTypeAssignmentService(new EmptyTypeSuggestions())
            .AssignAsync(analysis, null, TestContext.Current.CancellationToken);
        Assert.True(assignment.IsSuccess);

        var snapshot = new CatalogRecognitionRuleSetExecutionSnapshot(
            Guid.NewGuid(), manufacturerId, productType.Id, 1,
            [new CatalogRecognitionLiteralExecutionRule(Guid.NewGuid(), definition.Id, "literal-v1", "х-ка D", "D")], [], []);
        var enrichment = await new CatalogImportRecognitionEnrichmentService(
                new EmptyRecognition(), new CatalogImportRowValidator(), new FakeActiveRuleSetReader(snapshot))
            .EnrichAsync(assignment.Value, productType, [definition], TestContext.Current.CancellationToken);
        Assert.True(enrichment.IsSuccess);
        var row = Assert.Single(enrichment.Value.Analysis.Rows);
        var actual = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions)!;
        Assert.Equal(saved.Id, row.Id);
        Assert.Equal(manufacturerId, actual.ManufacturerId);
        Assert.Equal(productType.Id, actual.ProductTypeId);
        Assert.Equal("Manual", actual.ProductTypeResolutionSource);
        Assert.Equal(123m, actual.Price);
        Assert.Equal(7, actual.StockQuantity);
        if (manualValue is not null)
        {
            Assert.Equal(CatalogImportCharacteristicValueSource.Manual, actual.CharacteristicOrigins![key].Source);
        }
        if (string.IsNullOrEmpty(manualValue) && manualValue is not null)
        {
            Assert.False(actual.Characteristics.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value));
        }
        else
        {
            Assert.Equal(manualValue ?? "D", actual.Characteristics[key]);
        }
    }

    private static CatalogImportRow CreateRow(Guid batchId, string rawJson, CatalogImportNormalizedRowData? data = null)
    {
        data ??= new CatalogImportNormalizedRowData("Автомат", "1", null, null, null,
            new Dictionary<string, string>(StringComparer.Ordinal));
        var result = CatalogImportRow.Create(batchId, 2, CatalogImportRowStatus.Error, rawJson,
            JsonSerializer.Serialize(data, JsonOptions), "[]", "[]");
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task LowConfidenceCandidateIsVisibleButNotAppliedAndWarningIsRefreshed()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition, isRequired: true);
        var data = new CatalogImportNormalizedRowData("Автомат 16A", "1", "CHINT", null, null,
            new Dictionary<string, string>(StringComparer.Ordinal), Guid.NewGuid(), ProductTypeId: productType.Id);
        var row = CreateRow(Guid.NewGuid(), "{}", data);
        var analysis = new CatalogImportWorkbookAnalysis([], [row], false, 0, 1,
            new CatalogImportManufacturerResolutionSummary(0, 0, 0, 0, 0, []));
        var candidate = new CatalogRecognizedCharacteristic(definition.Code, "16A", "16", 0.96m,
            CatalogRecognitionSource.Dictionary, 8, 3, 100, "test:dictionary");
        var service = new CatalogImportRecognitionEnrichmentService(new EmptyRecognition(candidate),
            new CatalogImportRowValidator(), new FakeActiveRuleSetReader());
        var result = await service.EnrichAsync(analysis, productType, [definition], TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        var actual = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions)!;
        Assert.False(actual.Characteristics.ContainsKey(definition.Id.ToString()));
        Assert.Equal("16", actual.CharacteristicRecognitionSuggestions![definition.Id.ToString()].NormalizedValue);
        Assert.Contains("16", row.WarningsJson, StringComparison.Ordinal);

        var confidentService = new CatalogImportRecognitionEnrichmentService(
            new EmptyRecognition(candidate with { Confidence = 0.99m }),
            new CatalogImportRowValidator(), new FakeActiveRuleSetReader());
        var rerun = await confidentService.EnrichAsync(result.Value.Analysis, productType, [definition], TestContext.Current.CancellationToken);
        Assert.True(rerun.IsSuccess);
        actual = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions)!;
        Assert.Equal("16", actual.Characteristics[definition.Id.ToString()]);
        Assert.DoesNotContain("characteristic.low_confidence", row.WarningsJson, StringComparison.Ordinal);
    }

    private sealed class EmptyRecognition(CatalogRecognizedCharacteristic? candidate = null) : ICatalogProductNameRecognitionService
    {
        public Task<CatalogProductNameRecognitionResult> RecognizeAsync(
            CatalogProductNameRecognitionRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CatalogRecognizedCharacteristic[] candidates = candidate is null ? [] : [candidate];
            return Task.FromResult(new CatalogProductNameRecognitionResult(request.ProductName, request.ProductName, candidates, [], candidates));
        }
    }

    private sealed class EmptyTypeSuggestions : ICatalogProductTypeSuggestionService
    {
        public Task<CatalogProductTypeSuggestionIndex> LoadIndexAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CatalogProductTypeSuggestionIndex([], []));
        }

        public Task<CatalogProductTypeSuggestionResult> SuggestAsync(string productName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CatalogProductTypeSuggestionIndex([], []).Suggest(productName, cancellationToken));
        }
    }
}
