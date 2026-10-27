using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.TestCommon;
using System.Text.Json;

namespace ElectronicService.Core.UnitTests.Catalog.ImportBatches;

public sealed class CatalogImportAnalogyDecisionServiceTests
{
    private readonly Guid _manufacturerId = Guid.CreateVersion7();
    private readonly Guid _batchId = Guid.CreateVersion7();
    private readonly Guid _rowId = Guid.CreateVersion7();

    [Fact]
    public async Task ApplyAsyncStoresAcceptedDecisionAndCatalogAnalogyOrigin()
    {
        var scenario = CreateScenario();

        var result = await scenario.Service.ApplyAsync(
            _batchId,
            _rowId,
            scenario.ProductType,
            [scenario.Definition],
            scenario.Before,
            scenario.AfterWithProposedValue,
            [new CatalogImportAnalogyDecision(
                scenario.Definition.Id,
                CatalogImportAnalogyDecisionKind.Accepted,
                Reason: null)],
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            CatalogImportCharacteristicValueSource.CatalogAnalogy,
            result.Value.CharacteristicOrigins![scenario.Definition.Id.ToString()].Source);
        Assert.NotNull(
            result.Value.CharacteristicOrigins![scenario.Definition.Id.ToString()]
                .AnalogyCandidateSnapshotId);
        Assert.Equal(
            3,
            result.Value.CharacteristicOrigins![scenario.Definition.Id.ToString()]
                .AnalogySupportingProductCount);

        var feedback = Assert.Single(scenario.FeedbackRepository.Added);
        Assert.Equal(CatalogRecognitionFeedbackType.Accepted, feedback.FeedbackType);
        Assert.Equal("CatalogAnalogy", feedback.SuggestedSource);
        Assert.Equal("B", feedback.SuggestedNormalizedValue);
        Assert.Equal("B", feedback.FinalNormalizedValue);
        Assert.Contains(
            "supportingProductIds",
            feedback.EvidenceSnapshotJson,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsyncStoresRejectedDecisionWithReason()
    {
        var scenario = CreateScenario();

        var result = await scenario.Service.ApplyAsync(
            _batchId,
            _rowId,
            scenario.ProductType,
            [scenario.Definition],
            scenario.Before,
            scenario.Before,
            [new CatalogImportAnalogyDecision(
                scenario.Definition.Id,
                CatalogImportAnalogyDecisionKind.Rejected,
                "Не соответствует серии")],
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        var feedback = Assert.Single(scenario.FeedbackRepository.Added);
        Assert.Equal(CatalogRecognitionFeedbackType.Rejected, feedback.FeedbackType);
        Assert.Null(feedback.FinalNormalizedValue);
        using var evidence = JsonDocument.Parse(feedback.EvidenceSnapshotJson!);
        Assert.Equal(
            "Не соответствует серии",
            evidence.RootElement.GetProperty("rejectionReason").GetString());
    }

    [Fact]
    public async Task ApplyAsyncRejectsDecisionWhenCurrentEvidenceIsInsufficient()
    {
        var scenario = CreateScenario(candidateCount: 2);

        var result = await scenario.Service.ApplyAsync(
            _batchId,
            _rowId,
            scenario.ProductType,
            [scenario.Definition],
            scenario.Before,
            scenario.AfterWithProposedValue,
            [new CatalogImportAnalogyDecision(
                scenario.Definition.Id,
                CatalogImportAnalogyDecisionKind.Accepted,
                Reason: null)],
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(scenario.FeedbackRepository.Added);
    }

    [Fact]
    public async Task FinalizerDoesNotReturnAnalogyDecisionToTrainingSample()
    {
        var scenario = CreateScenario();

        var applyResult = await scenario.Service.ApplyAsync(
            _batchId,
            _rowId,
            scenario.ProductType,
            [scenario.Definition],
            scenario.Before,
            scenario.AfterWithProposedValue,
            [new CatalogImportAnalogyDecision(
                scenario.Definition.Id,
                CatalogImportAnalogyDecisionKind.Accepted,
                Reason: null)],
            TestContext.Current.CancellationToken);

        Assert.True(applyResult.IsSuccess);

        var finalizeResult = await new CatalogImportRecognitionFeedbackFinalizer(
                scenario.FeedbackRepository)
            .FinalizeForAppliedBatchAsync(
                _batchId,
                Guid.CreateVersion7(),
                UserType.Technical,
                TestContext.Current.CancellationToken);

        Assert.True(finalizeResult.IsSuccess);
        var feedback = Assert.Single(scenario.FeedbackRepository.Added);
        Assert.True(feedback.IsFinalized);
        Assert.False(feedback.IsTrainingEligible);
    }

    private Scenario CreateScenario(int candidateCount = 3)
    {
        var productType = TestDataFactory.CreateProductType();
        var definition = TestDataFactory.CreateCharacteristicDefinition(
            "CURVE",
            "Характеристика срабатывания",
            CharacteristicDataType.Text,
            unit: null);

        TestDataFactory.AddCharacteristic(productType, definition);

        var candidates = Enumerable.Range(0, candidateCount)
            .Select(_ => new CatalogProductAnalogyCandidate(
                Guid.CreateVersion7(),
                "NB1",
                "CHINT NB1 1P 16A",
                _manufacturerId,
                productType.Id,
                productType.Code,
                productType.Name,
                Aliases: [],
                Characteristics: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [definition.Code] = "B"
                },
                IsArchived: false))
            .ToArray();

        var reader = new StubAnalogyReader(candidates);
        var feedbackRepository = new StubFeedbackRepository();
        var service = new CatalogImportAnalogyDecisionService(
            new PreviewCatalogProductAnalogiesQueryHandler(reader),
            feedbackRepository);

        var before = new CatalogImportNormalizedRowData(
            "CHINT NB1 1P 16A",
            "NB1",
            "CHINT",
            Price: null,
            StockQuantity: null,
            Characteristics: new Dictionary<string, string>(StringComparer.Ordinal),
            ManufacturerId: _manufacturerId,
            ProductTypeId: productType.Id);

        var after = before with
        {
            Characteristics = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [definition.Id.ToString()] = "B"
            }
        };

        return new Scenario(
            service,
            feedbackRepository,
            productType,
            definition,
            before,
            after);
    }

    private sealed class StubAnalogyReader(
        IReadOnlyCollection<CatalogProductAnalogyCandidate> candidates)
        : ICatalogProductAnalogyReader
    {
        public Task<CatalogProductAnalogyCandidateSnapshot>
            ReadCandidateSnapshotAsync(
                Guid manufacturerId,
                Guid? productTypeId,
                int maximumCandidates,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new CatalogProductAnalogyCandidateSnapshot(
                    "test-snapshot",
                    DateTime.UtcNow,
                    candidates));
        }
    }

    private sealed class StubFeedbackRepository
        : ICatalogRecognitionFeedbackRepository
    {
        public List<CatalogRecognitionFeedback> Added { get; } = [];

        public Task<CatalogRecognitionFeedback?> GetByIdAsync(
            Guid feedbackId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogRecognitionFeedback?>(null);

        public Task<CatalogRecognitionFeedback?>
            GetByImportRowAndCharacteristicAsync(
                Guid importRowId,
                Guid characteristicDefinitionId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogRecognitionFeedback?>(null);

        public Task<IReadOnlyCollection<CatalogRecognitionFeedback>>
            GetByImportRowAsync(
                Guid importRowId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CatalogRecognitionFeedback>>([]);

        public Task<IReadOnlyCollection<CatalogRecognitionFeedback>>
            GetPendingByImportBatchAsync(
                Guid importBatchId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CatalogRecognitionFeedback>>(
                Added
                    .Where(feedback =>
                        feedback.ImportBatchId == importBatchId
                        && feedback.IsPending)
                    .ToArray());

        public Task<bool> ExistsForImportRowAndCharacteristicAsync(
            Guid importRowId,
            Guid characteristicDefinitionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Add(CatalogRecognitionFeedback feedback) => Added.Add(feedback);

        public void Remove(CatalogRecognitionFeedback feedback) => Added.Remove(feedback);

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record Scenario(
        CatalogImportAnalogyDecisionService Service,
        StubFeedbackRepository FeedbackRepository,
        ProductType ProductType,
        CharacteristicDefinition Definition,
        CatalogImportNormalizedRowData Before,
        CatalogImportNormalizedRowData AfterWithProposedValue);
}
