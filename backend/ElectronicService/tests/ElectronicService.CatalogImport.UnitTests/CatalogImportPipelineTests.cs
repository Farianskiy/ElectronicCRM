using System.Text.Json;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches;
using ElectronicService.TestCommon;

namespace ElectronicService.CatalogImport.UnitTests;

public sealed class CatalogImportPipelineTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    [Fact]
    public void ReadyMixedBatchCanBeSubmittedWithoutBatchWideProductType()
    {
        var batchResult = CatalogImportBatch.Create(
            Guid.NewGuid(),
            "mixed.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [1]);

        Assert.True(batchResult.IsSuccess);

        var analysisResult = batchResult.Value.RegisterAnalysisResult(
            1,
            1,
            0,
            false);

        Assert.True(analysisResult.IsSuccess);
        Assert.Null(batchResult.Value.ProductTypeId);
        Assert.True(batchResult.Value.SubmitForReview().IsSuccess);
        Assert.Equal(CatalogImportBatchStatus.Submitted, batchResult.Value.Status);
    }

    [Fact]
    public void ReadyMixedBatchCanStartApplyingWithoutBatchWideProductType()
    {
        var batchResult = CatalogImportBatch.Create(
            Guid.NewGuid(),
            "mixed.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [1]);

        Assert.True(batchResult.IsSuccess);
        Assert.True(batchResult.Value.RegisterAnalysisResult(1, 1, 0, false).IsSuccess);
        Assert.True(batchResult.Value.StartApplying(Guid.NewGuid()).IsSuccess);
        Assert.Equal(CatalogImportBatchStatus.Applying, batchResult.Value.Status);
    }

    [Fact]
    public async Task MixedWorkbookResolvesManufacturerTypeAndRequiredCharacteristicFromName()
    {
        var manufacturerId = Guid.NewGuid();
        var manufacturerIndex = new ManufacturerResolutionIndex(
            [new ManufacturerResolutionEntry(
                manufacturerId,
                "CHINT",
                "CHINT",
                ManufacturerResolutionSource.ExactName,
                null)],
            []);

        var analysisResult = new CatalogImportWorkbookAnalyzer().Analyze(
            Guid.NewGuid(),
            CreateWorkbook("NB1-63", "Автомат CHINT 16A"),
            null,
            [],
            manufacturerIndex,
            [],
            TestContext.Current.CancellationToken);

        Assert.True(analysisResult.IsSuccess);
        Assert.False(analysisResult.Value.MappingRequired);

        var initialRow = Assert.Single(analysisResult.Value.Rows);
        Assert.Equal(manufacturerId, DeserializeData(initialRow).ManufacturerId);
        Assert.Contains(
            DeserializeIssues(initialRow.IssuesJson),
            issue => string.Equals(
                issue.Code,
                "product_type.required",
                StringComparison.Ordinal));

        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition, isRequired: true);

        var suggestionIndex = new CatalogProductTypeSuggestionIndex(
            [new CatalogProductTypeResult(
                productType.Id,
                productType.Code,
                productType.Name)],
            [CreateProductTypeTerm(productType)]);

        var assignmentResult = await new CatalogImportProductTypeAssignmentService(
                new FakeProductTypeSuggestionService(suggestionIndex))
            .AssignAsync(
                analysisResult.Value,
                null,
                TestContext.Current.CancellationToken);

        Assert.True(assignmentResult.IsSuccess);

        var enrichmentResult = await new CatalogImportRecognitionEnrichmentService(
                new FakeRecognitionService(CreateRecognizedCharacteristic(definition.Code)),
                new CatalogImportRowValidator())
            .EnrichAsync(
                assignmentResult.Value,
                productType,
                [definition],
                TestContext.Current.CancellationToken);

        Assert.True(enrichmentResult.IsSuccess);

        var row = Assert.Single(enrichmentResult.Value.Analysis.Rows);
        var data = DeserializeData(row);

        Assert.Equal(CatalogImportRowStatus.Valid, row.Status);
        Assert.Equal(productType.Id, data.ProductTypeId);
        Assert.Equal("Automatic", data.ProductTypeResolutionSource);
        Assert.Equal(manufacturerId, data.ManufacturerId);
        Assert.Equal("16", data.Characteristics[definition.Id.ToString()]);
        Assert.Empty(DeserializeIssues(row.IssuesJson));
    }

    [Fact]
    public async Task EnrichmentMarksConflictBetweenExcelAndProductNameValues()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition);

        var data = new CatalogImportNormalizedRowData(
            "Автомат 16A",
            "NB1-16",
            "CHINT",
            null,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [definition.Id.ToString()] = "25"
            },
            Guid.NewGuid(),
            ProductTypeId: productType.Id,
            ProductTypeResolutionSource: "Manual",
            ProductTypeResolutionConfidence: 1.0000m);

        var rowResult = CatalogImportRow.Create(
            Guid.NewGuid(),
            2,
            CatalogImportRowStatus.Valid,
            "{}",
            JsonSerializer.Serialize(data, JsonOptions),
            "[]",
            "[]");

        Assert.True(rowResult.IsSuccess);

        var analysis = new CatalogImportWorkbookAnalysis(
            [],
            [rowResult.Value],
            false,
            1,
            0,
            new CatalogImportManufacturerResolutionSummary(
                0,
                0,
                0,
                0,
                0,
                []));

        var result = await new CatalogImportRecognitionEnrichmentService(
                new FakeRecognitionService(CreateRecognizedCharacteristic(definition.Code)),
                new CatalogImportRowValidator())
            .EnrichAsync(
                analysis,
                productType,
                [definition],
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        var row = Assert.Single(result.Value.Analysis.Rows);
        var conflict = Assert.Single(
            DeserializeIssues(row.IssuesJson),
            issue => string.Equals(
                issue.Code,
                "characteristic.value_conflict",
                StringComparison.Ordinal));

        Assert.Equal(CatalogImportRowStatus.Error, row.Status);
        Assert.Equal(definition.Id.ToString(), conflict.Field);
    }

    private static CatalogRecognizedCharacteristic CreateRecognizedCharacteristic(
        string code)
    {
        return new CatalogRecognizedCharacteristic(
            code,
            "16A",
            "16",
            0.9900m,
            CatalogRecognitionSource.Dictionary,
            8,
            3,
            100,
            "test:dictionary");
    }

    private static byte[] CreateWorkbook(string article, string name)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Товары");
        worksheet.Cell(1, 1).Value = "Артикул";
        worksheet.Cell(1, 2).Value = "Наименование";
        worksheet.Cell(2, 1).Value = article;
        worksheet.Cell(2, 2).Value = name;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static CatalogDictionaryTermResult CreateProductTypeTerm(
        ElectronicService.Domain.Catalog.ProductTypes.ProductType productType)
    {
        return new CatalogDictionaryTermResult(
            Guid.NewGuid(),
            null,
            productType.Id,
            "Автомат",
            "АВТОМАТ",
            "ProductType",
            null,
            productType.Code,
            100,
            "Approved",
            "Test",
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

    private static CatalogImportNormalizedRowData DeserializeData(CatalogImportRow row)
    {
        return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                   row.NormalizedDataJson,
                   JsonOptions)
               ?? throw new InvalidOperationException("Normalized row is empty.");
    }

    private static CatalogImportRowIssue[] DeserializeIssues(string json)
    {
        return JsonSerializer.Deserialize<CatalogImportRowIssue[]>(json, JsonOptions)
               ?? [];
    }

    private sealed class FakeProductTypeSuggestionService(
        CatalogProductTypeSuggestionIndex index)
        : ICatalogProductTypeSuggestionService
    {
        public Task<CatalogProductTypeSuggestionIndex> LoadIndexAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(index);
        }

        public Task<CatalogProductTypeSuggestionResult> SuggestAsync(
            string productName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(index.Suggest(productName, cancellationToken));
        }
    }

    private sealed class FakeRecognitionService(
        CatalogRecognizedCharacteristic characteristic)
        : ICatalogProductNameRecognitionService
    {
        public Task<CatalogProductNameRecognitionResult> RecognizeAsync(
            CatalogProductNameRecognitionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                new CatalogProductNameRecognitionResult(
                    request.ProductName,
                    request.ProductName.ToUpperInvariant(),
                    [characteristic],
                    [],
                    [characteristic]));
        }
    }
}
