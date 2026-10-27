using System.Text.Json;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.Products.Analogies;
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
    public void UnifiedWorkbookUsesProductsWorksheetEvenWhenRulesComeFirst()
    {
        using var workbook = new XLWorkbook();
        var rules = workbook.AddWorksheet("Совместимость");
        rules.Cell(1, 1).Value = "Артикул комплектующего";
        rules.Cell(1, 2).Value = "Тип основного товара";
        rules.Cell(1, 3).Value = "Потребность";
        rules.Cell(2, 1).Value = "COMP-1";
        rules.Cell(2, 2).Value = "BREAKER";

        var products = workbook.AddWorksheet("Товары");
        products.Cell(1, 1).Value = "Артикул";
        products.Cell(1, 2).Value = "Наименование";
        products.Cell(2, 1).Value = "MAIN-1";
        products.Cell(2, 2).Value = "Основной товар";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var analysisResult = new CatalogImportWorkbookAnalyzer().Analyze(
            Guid.NewGuid(),
            stream.ToArray(),
            null,
            [],
            new ManufacturerResolutionIndex([], []),
            [],
            TestContext.Current.CancellationToken);

        Assert.True(analysisResult.IsSuccess);
        var row = Assert.Single(analysisResult.Value.Rows);
        Assert.Equal("MAIN-1", DeserializeData(row).Article);
    }

    [Fact]
    public async Task ComponentRoleUsesGenericComponentTypeFromWorkbook()
    {
        var manufacturerId = Guid.NewGuid();
        var manufacturerIndex = new ManufacturerResolutionIndex(
            [new ManufacturerResolutionEntry(
                manufacturerId,
                "DEKraft",
                "DEKRAFT",
                ManufacturerResolutionSource.ExactName,
                null)],
            []);
        using var workbook = new XLWorkbook();
        var products = workbook.AddWorksheet("Товары");
        products.Cell(1, 1).Value = "Производитель";
        products.Cell(1, 2).Value = "Артикул";
        products.Cell(1, 3).Value = "Наименование";
        products.Cell(1, 4).Value = "Назначение товара";
        products.Cell(2, 1).Value = "DEKraft";
        products.Cell(2, 2).Value = "COMP-1";
        products.Cell(2, 3).Value = "Дополнительный контакт";
        products.Cell(2, 4).Value = "Комплектующее";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var analysisResult = new CatalogImportWorkbookAnalyzer().Analyze(
            Guid.NewGuid(),
            stream.ToArray(),
            null,
            [],
            manufacturerIndex,
            [],
            TestContext.Current.CancellationToken);

        Assert.True(analysisResult.IsSuccess);
        var componentType = TestDataFactory.CreateProductType(
            "COMPONENT_GENERIC",
            "Комплектующие");
        var assignmentResult = await new CatalogImportProductTypeAssignmentService(
                new FakeProductTypeSuggestionService(
                    new CatalogProductTypeSuggestionIndex(
                        [new CatalogProductTypeResult(
                            componentType.Id,
                            componentType.Code,
                            componentType.Name)],
                        [])))
            .AssignAsync(
                analysisResult.Value,
                null,
                TestContext.Current.CancellationToken);

        Assert.True(assignmentResult.IsSuccess);
        var data = DeserializeData(Assert.Single(assignmentResult.Value.Rows));
        Assert.Equal(componentType.Id, data.ProductTypeId);
        Assert.Equal("ExcelProductType", data.ProductTypeResolutionSource);
        Assert.Equal("Комплектующее", data.ProductRole);
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
                FakeActiveRuleSetReader.Effective(new FakeRecognitionService(CreateRecognizedCharacteristic(definition.Code))),
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
    public void ExplicitProductTypeNameWinsOverGenericDictionaryTerm()
    {
        var modularType = new CatalogProductTypeResult(
            Guid.NewGuid(),
            "MODULAR_CIRCUIT_BREAKER",
            "Модульный автомат");
        var motorType = new CatalogProductTypeResult(
            Guid.NewGuid(),
            "MOTOR_CIRCUIT_BREAKER",
            "Мотор-автомат");

        var genericTerm = new CatalogDictionaryTermResult(
            Guid.NewGuid(),
            null,
            modularType.Id,
            "Автомат",
            "АВТОМАТ",
            "ProductType",
            null,
            modularType.Code,
            10,
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

        var result = new CatalogProductTypeSuggestionIndex(
                [modularType, motorType],
                [genericTerm])
            .Suggest(
                "Мотор-автомат ПРК32 8А",
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuggested);
        Assert.NotNull(result.SelectedCandidate);
        Assert.Equal(motorType.Id, result.SelectedCandidate.ProductTypeId);
        Assert.Equal(0.9900m, result.SelectedCandidate.Confidence);
    }

    [Fact]
    public void CanonicalProductTypeNameWorksWithoutDictionaryTerm()
    {
        var rcdType = new CatalogProductTypeResult(
            Guid.NewGuid(),
            "RCD",
            "УЗО");

        var result = new CatalogProductTypeSuggestionIndex(
                [rcdType],
                [])
            .Suggest(
                "УЗО ВД1-63 2P 25A 30мА",
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuggested);
        Assert.NotNull(result.SelectedCandidate);
        Assert.Equal(rcdType.Id, result.SelectedCandidate.ProductTypeId);
        Assert.Equal(0.9900m, result.SelectedCandidate.Confidence);
    }

    [Fact]
    public async Task MixedWorkbookResolvesProductTypeFromExistingCatalogFamily()
    {
        var manufacturerId = Guid.NewGuid();
        var productType = TestDataFactory.CreateProductType();
        var manufacturerIndex = new ManufacturerResolutionIndex(
            [new ManufacturerResolutionEntry(
                manufacturerId,
                "IEK",
                "IEK",
                ManufacturerResolutionSource.ExactName,
                null)],
            []);

        var analysisResult = new CatalogImportWorkbookAnalyzer().Analyze(
            Guid.NewGuid(),
            CreateWorkbook("QA-MOTOR-NEW", "ПРК32 12 12 Ir=8-12А Кнопки IEK"),
            null,
            [],
            manufacturerIndex,
            [],
            TestContext.Current.CancellationToken);

        Assert.True(analysisResult.IsSuccess);

        var candidates = new[]
        {
            CreateAnalogyCandidate("ПРК32 10 10 Ir=6-10А Кнопки IEK"),
            CreateAnalogyCandidate("ПРК32 16 16 Ir=10-16А Кнопки IEK"),
            CreateAnalogyCandidate("ПРК32 20 20 Ir=14-20А Кнопки IEK")
        };

        var assignmentResult = await new CatalogImportProductTypeAssignmentService(
                new FakeProductTypeSuggestionService(
                    new CatalogProductTypeSuggestionIndex(
                        [new CatalogProductTypeResult(
                            productType.Id,
                            productType.Code,
                            productType.Name)],
                        [])),
                new FakeAnalogyReader(candidates))
            .AssignAsync(
                analysisResult.Value,
                null,
                TestContext.Current.CancellationToken);

        Assert.True(assignmentResult.IsSuccess);

        var row = Assert.Single(assignmentResult.Value.Rows);
        var data = DeserializeData(row);
        Assert.Equal(productType.Id, data.ProductTypeId);
        Assert.Equal("CatalogAnalogy", data.ProductTypeResolutionSource);
        Assert.Equal(1m, data.ProductTypeResolutionConfidence);
        Assert.Contains(
            DeserializeIssues(row.WarningsJson),
            warning => string.Equals(
                warning.Code,
                "product_type.resolved_from_catalog_examples",
                StringComparison.Ordinal));

        CatalogProductAnalogyCandidate CreateAnalogyCandidate(string name)
        {
            return new CatalogProductAnalogyCandidate(
                Guid.NewGuid(),
                Guid.NewGuid().ToString("N"),
                name,
                manufacturerId,
                productType.Id,
                productType.Code,
                productType.Name,
                [],
                new Dictionary<string, string>(StringComparer.Ordinal),
                IsArchived: false);
        }
    }

    [Fact]
    public void WorkbookWithOnlyArticleAndNameDoesNotRequireCharacteristicColumns()
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

        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition, isRequired: true);

        var analysisResult = new CatalogImportWorkbookAnalyzer().Analyze(
            Guid.NewGuid(),
            CreateWorkbook("NB1-63", "Автомат CHINT без распознанного тока"),
            productType,
            [definition],
            manufacturerIndex,
            [],
            TestContext.Current.CancellationToken);

        Assert.True(analysisResult.IsSuccess);
        Assert.False(analysisResult.Value.MappingRequired);

        var row = Assert.Single(analysisResult.Value.Rows);
        Assert.Equal(CatalogImportRowStatus.Valid, row.Status);
        Assert.DoesNotContain(
            DeserializeIssues(row.IssuesJson),
            issue => string.Equals(
                issue.Code,
                "characteristic.required",
                StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequiredCharacteristicIsAQualityWarningInsteadOfImportError()
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition, isRequired: true);

        var validationResult = new CatalogImportRowValidator().Validate(
            new CatalogImportNormalizedRowData(
                "Автомат CHINT",
                "NB1-63",
                "CHINT",
                null,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal),
                Guid.NewGuid(),
                ProductTypeId: productType.Id),
            productType,
            [definition]);

        Assert.Equal(CatalogImportRowStatus.Valid, validationResult.Status);
        Assert.Empty(validationResult.Issues);
        Assert.Contains(
            validationResult.Warnings,
            warning => string.Equals(
                warning.Code,
                "characteristic.required",
                StringComparison.Ordinal));
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
                FakeActiveRuleSetReader.Effective(new FakeRecognitionService(CreateRecognizedCharacteristic(definition.Code))),
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

    [Fact]
    public async Task EnrichmentLoadsAnalogyScopeOnceForRowsInSameBatchScope()
    {
        var manufacturerId = Guid.NewGuid();
        var productType = TestDataFactory.CreateProductType();
        var rows = new[]
        {
            CreateRow(2, "NB1-63 1P 10A"),
            CreateRow(3, "NB1-63 1P 16A")
        };
        var analysis = new CatalogImportWorkbookAnalysis(
            [],
            rows,
            false,
            rows.Length,
            0,
            new CatalogImportManufacturerResolutionSummary(0, 0, 0, 0, 0, []));
        var analogyReader = new FakeAnalogyReader([]);
        var analogyPreview = new PreviewCatalogProductAnalogiesQueryHandler(analogyReader);
        var unusedRecognition = CreateRecognizedCharacteristic("UNKNOWN");

        var result = await new CatalogImportRecognitionEnrichmentService(
                FakeActiveRuleSetReader.Effective(
                    new FakeRecognitionService(unusedRecognition)),
                new CatalogImportRowValidator(),
                analogyPreview)
            .EnrichAsync(
                analysis,
                productType,
                [],
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, analogyReader.Calls);

        CatalogImportRow CreateRow(int rowNumber, string name)
        {
            var data = new CatalogImportNormalizedRowData(
                name,
                $"TEST-{rowNumber}",
                "CHINT",
                null,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal),
                manufacturerId,
                ProductTypeId: productType.Id,
                ProductTypeResolutionSource: "Automatic",
                ProductTypeResolutionConfidence: 1m);
            var row = CatalogImportRow.Create(
                Guid.NewGuid(),
                rowNumber,
                CatalogImportRowStatus.Valid,
                "{}",
                JsonSerializer.Serialize(data, JsonOptions),
                "[]",
                "[]");
            Assert.True(row.IsSuccess);
            return row.Value;
        }
    }

    [Fact]
    public async Task ReanalysisRemovesCatalogAnalogyValueWhenCurrentCatalogNoLongerSupportsIt()
    {
        var manufacturerId = Guid.NewGuid();
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition();
        TestDataFactory.AddCharacteristic(productType, definition);
        var definitionKey = definition.Id.ToString();
        var data = new CatalogImportNormalizedRowData(
            "NB1-63 специальное исполнение",
            "NB1-TEST",
            "CHINT",
            null,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [definitionKey] = "B"
            },
            ManufacturerId: manufacturerId,
            CharacteristicOrigins: new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal)
            {
                [definitionKey] = CatalogImportCharacteristicValueOrigin.FromCatalogAnalogy(
                    "B",
                    1m,
                    Guid.NewGuid(),
                    1,
                    [Guid.NewGuid()],
                    "old-configuration",
                    "old-snapshot",
                    DateTime.UtcNow.AddDays(-1),
                    "NB1-63",
                    1)
            },
            ProductTypeId: productType.Id,
            ProductTypeResolutionSource: "Manual",
            ProductTypeResolutionConfidence: 1m);
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
            new CatalogImportManufacturerResolutionSummary(0, 0, 0, 0, 0, []));
        var analogyPreview = new PreviewCatalogProductAnalogiesQueryHandler(
            new FakeAnalogyReader([]));

        var result = await new CatalogImportRecognitionEnrichmentService(
                FakeActiveRuleSetReader.Effective(
                    new FakeRecognitionService(CreateRecognizedCharacteristic("UNKNOWN"))),
                new CatalogImportRowValidator(),
                analogyPreview)
            .EnrichAsync(
                analysis,
                productType,
                [definition],
                TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var enrichedRow = Assert.Single(result.Value.Analysis.Rows);
        var enrichedData = DeserializeData(enrichedRow);
        Assert.DoesNotContain(definitionKey, enrichedData.Characteristics);
        Assert.False(enrichedData.CharacteristicOrigins?.ContainsKey(definitionKey) ?? false);
        Assert.Contains(
            DeserializeIssues(enrichedRow.WarningsJson),
            warning => string.Equals(
                    warning.Code,
                    "characteristic.analogy_stale",
                    StringComparison.Ordinal)
                && string.Equals(
                    warning.Field,
                    definitionKey,
                    StringComparison.Ordinal));
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

    private sealed class FakeAnalogyReader(
        IReadOnlyCollection<CatalogProductAnalogyCandidate> candidates)
        : ICatalogProductAnalogyReader
    {
        public int Calls { get; private set; }

        public Task<CatalogProductAnalogyCandidateSnapshot>
            ReadCandidateSnapshotAsync(
                Guid manufacturerId,
                Guid? productTypeId,
                int maximumCandidates,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;

            return Task.FromResult(
                new CatalogProductAnalogyCandidateSnapshot(
                    $"test:{manufacturerId:N}",
                    DateTime.UtcNow,
                    candidates.Take(maximumCandidates).ToArray()));
        }
    }
}
