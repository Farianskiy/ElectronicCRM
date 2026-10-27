using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.UnitTests.Catalog.Products.Analogies;

public sealed class CatalogRecognitionFeedbackAnalogyLifecycleTests
{
    [Fact]
    public void ShownObservationCanBeRecordedAsAccepted()
    {
        var feedback = CreateShown("evidence-1");

        var result = feedback.RecordAnalogyDecision(
            CatalogRecognitionFeedbackType.Accepted,
            "B",
            "evidence-accepted");

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogRecognitionFeedbackType.Accepted, feedback.FeedbackType);
        Assert.Equal("B", feedback.FinalNormalizedValue);
        Assert.Equal(1, feedback.AnalogyDisplayCount);
        Assert.NotNull(feedback.AnalogyDecisionRecordedAtUtc);
        Assert.Equal("evidence-accepted", feedback.EvidenceSnapshotJson);
    }

    [Fact]
    public void ChangedObservationPreservesCountersAndReturnsToShownState()
    {
        var feedback = CreateShown("evidence-1");

        var result = feedback.RefreshAnalogyObservation(
            "NB1 1P 16A C",
            "NB1 1P 16A C",
            feedback.ManufacturerId!.Value,
            feedback.ProductTypeId,
            feedback.ProductTypeCodeSnapshot,
            "C",
            "C",
            0.9m,
            "CatalogAnalogy",
            "catalog-analogy-v1",
            "evidence-2",
            "Изменились входные данные.");

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogRecognitionFeedbackType.Shown, feedback.FeedbackType);
        Assert.Equal(2, feedback.AnalogyDisplayCount);
        Assert.Equal(1, feedback.AnalogyStaleCount);
        Assert.Equal("C", feedback.SuggestedNormalizedValue);
        Assert.Equal("Изменились входные данные.", feedback.AnalogyLastStaleReason);
    }

    [Fact]
    public void IdenticalObservationCanBeDetectedWithoutIncreasingDisplayCount()
    {
        var feedback = CreateShown("evidence-1");

        Assert.True(feedback.MatchesAnalogyObservation(
            feedback.NormalizedProductName,
            "B",
            "catalog-analogy-v1",
            "evidence-1"));
        Assert.Equal(1, feedback.AnalogyDisplayCount);
    }

    private static CatalogRecognitionFeedback CreateShown(string evidence)
    {
        var result = CatalogRecognitionFeedback.Create(
            "NB1 1P 10A B",
            "NB1 1P 10A B",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MODULAR_BREAKER",
            Guid.NewGuid(),
            "TRIP_CURVE",
            "B",
            "B",
            0.95m,
            "CatalogAnalogy",
            spanStart: null,
            spanLength: null,
            CatalogRecognitionFeedbackType.Shown,
            finalNormalizedValue: null,
            dictionaryTermId: null,
            recognitionProfileId: null,
            "catalog-analogy-v1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            evidence);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
